import { css, html, customElement, state, nothing, repeat } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { UrlInspectorService } from './api/index.js';
import type { UrlInspectorResponse, TestUrlResponse } from './api/index.js';

/** Maps the (English) messages of the test-url endpoint to localization keys. */
const TEST_MESSAGE_KEYS: Record<string, string> = {
  'No base URL': 'ddUrlInspector_noBaseUrl',
  'Invalid URL': 'ddUrlInspector_invalidUrl',
  'Host not allowed': 'ddUrlInspector_notAllowed',
  'Request failed': 'ddUrlInspector_requestFailed',
  'Timed out': 'ddUrlInspector_timedOut',
  'Request cancelled': 'ddUrlInspector_cancelled',
};

/** Only http(s) URLs may be used as a link target. */
function safeHref(url: string | null | undefined): string | undefined {
  if (!url) return undefined;
  try {
    const parsed = new URL(url, window.location.origin);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:' ? parsed.href : undefined;
  } catch {
    return undefined;
  }
}

@customElement('dd-url-inspector')
export class DdUrlInspectorElement extends UmbLitElement {
  @state() private _data?: UrlInspectorResponse;
  @state() private _error = false;
  @state() private _testResults: Record<string, string> = {};

  #key?: string;
  /** Incremented per load so out-of-order responses (and responses for a previous document) are ignored. */
  #requestId = 0;

  constructor() {
    super();
    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (ctx) => {
      // Observing under a fixed alias replaces the previous observer when the context changes.
      this.observe(
        ctx?.unique,
        (unique) => {
          if (unique && unique !== this.#key) {
            this.#key = unique;
            void this.#load(unique);
          }
        },
        'ddUrlInspectorUnique',
      );
    });
  }

  override disconnectedCallback() {
    super.disconnectedCallback();
    // Invalidate in-flight requests so they do not update a detached element.
    this.#requestId++;
  }

  async #load(key: string) {
    const requestId = ++this.#requestId;
    this._error = false;
    this._data = undefined;
    this._testResults = {};
    let data: UrlInspectorResponse | undefined;
    try {
      const response = await UrlInspectorService.getUrlInspector({ path: { key } });
      data = response.error !== undefined ? undefined : response.data;
    } catch {
      data = undefined;
    }
    if (requestId !== this.#requestId) return; // stale response
    this._data = data;
    this._error = data === undefined;
  }

  async #copy(text: string | null | undefined) {
    if (!text) return;
    let ok = true;
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      ok = false;
    }
    const ctx = await this.getContext(UMB_NOTIFICATION_CONTEXT);
    ctx?.peek(ok ? 'positive' : 'danger', {
      data: { message: this.localize.term(ok ? 'ddUrlInspector_copied' : 'ddUrlInspector_copyFailed') },
    });
  }

  #describe(r: TestUrlResponse | undefined): string {
    if (!r) return this.localize.term('ddUrlInspector_failed');
    if (r.statusCode) return `${this.localize.term('ddUrlInspector_testResult')}: ${r.statusCode}`;
    const key = r.message ? TEST_MESSAGE_KEYS[r.message] : undefined;
    if (key) return this.localize.term(key);
    return r.allowed ? this.localize.term('ddUrlInspector_requestFailed') : this.localize.term('ddUrlInspector_notAllowed');
  }

  async #test(url: string) {
    const key = this.#key;
    if (!key) return;
    const requestId = this.#requestId;
    this._testResults = { ...this._testResults, [url]: this.localize.term('ddUrlInspector_testing') };
    let result: TestUrlResponse | undefined;
    try {
      const response = await UrlInspectorService.testUrl({ query: { documentKey: key }, body: { url } });
      result = response.error !== undefined ? undefined : response.data;
    } catch {
      result = undefined;
    }
    if (requestId !== this.#requestId) return; // document changed meanwhile
    this._testResults = { ...this._testResults, [url]: this.#describe(result) };
  }

  #none() {
    return html`<em>${this.localize.term('ddUrlInspector_none')}</em>`;
  }

  override render() {
    if (this._error) return html`<uui-box><p>${this.localize.term('ddUrlInspector_failed')}</p></uui-box>`;
    if (!this._data) return html`<uui-loader></uui-loader>`;
    const d = this._data;
    const allMissing = d.urls.every((u) => !u.url);
    return html`
      <uui-box headline=${this.localize.term('ddUrlInspector_currentUrls')}>
        ${allMissing
          ? html`<p>${this.localize.term('ddUrlInspector_noUrl')}</p>`
          : repeat(
              d.urls,
              (u) => `${u.culture ?? ''}|${u.url ?? 'missing'}`,
              (u) => !u.url
                ? html`<div class="line">
                    <span class="warn" title=${this.localize.term('ddUrlInspector_cultureNotPublished')}>⚠</span>
                    ${u.culture ? html`<uui-tag>${u.culture}</uui-tag>` : nothing}
                    <span class="missing">${u.message || this.localize.term('ddUrlInspector_cultureNotPublished')}</span>
                  </div>`
                : html` <div class="line">
                <span class="ok">✓</span>
                <span class="url">${u.url}</span>${u.culture ? html`<uui-tag>${u.culture}</uui-tag>` : nothing}
                <uui-button compact look="outline" label=${this.localize.term('ddUrlInspector_copy')} @click=${() => this.#copy(u.url)}
                  >${this.localize.term('ddUrlInspector_copy')}</uui-button
                >
                <uui-button compact look="outline" label=${this.localize.term('ddUrlInspector_test')} @click=${() => this.#test(u.url!)}
                  >${this.localize.term('ddUrlInspector_test')}</uui-button
                >
                ${safeHref(u.url)
                  ? html`<uui-button
                      compact
                      look="outline"
                      label=${this.localize.term('ddUrlInspector_open')}
                      href=${safeHref(u.url)!}
                      target="_blank"
                      rel="noopener noreferrer"
                      >${this.localize.term('ddUrlInspector_open')}</uui-button
                    >`
                  : nothing}
                <span class="test-result">${this._testResults[u.url!] ?? ''}</span>
              </div>`,
            )}
      </uui-box>

      <uui-box headline=${this.localize.term('ddUrlInspector_incoming')}>
        ${d.incomingRedirects.length === 0
          ? this.#none()
          : html`${d.incomingRedirects.map(
                (r) => html`<div class="line"><uui-tag>${r.statusCode}</uui-tag><span class="url">${r.from}</span></div>`,
              )}
              <uui-button
                look="outline"
                label=${this.localize.term('ddUrlInspector_copyRedirects')}
                @click=${() => this.#copy(d.incomingRedirects.map((r) => r.from).join('\n'))}
                >${this.localize.term('ddUrlInspector_copyRedirects')}</uui-button
              >`}
      </uui-box>

      <uui-box headline=${this.localize.term('ddUrlInspector_outgoing')}>
        ${d.outgoingRedirect
          ? html`<div class="line">
              <uui-tag>${d.outgoingRedirect.statusCode}</uui-tag
              ><span class="url">${d.outgoingRedirect.from} → ${d.outgoingRedirect.to ?? ''}</span>
            </div>`
          : this.#none()}
      </uui-box>

      <uui-box headline=${this.localize.term('ddUrlInspector_canonical')}>
        ${d.canonical
          ? html`<div class="line">
              <span class="url">${d.canonical}</span>
              <uui-button compact look="outline" label=${this.localize.term('ddUrlInspector_copy')} @click=${() => this.#copy(d.canonical)}
                >${this.localize.term('ddUrlInspector_copy')}</uui-button
              >
            </div>`
          : this.#none()}
      </uui-box>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }
    uui-box {
      margin-bottom: var(--uui-size-layout-1);
    }
    .line {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-2) 0;
      flex-wrap: wrap;
    }
    .url {
      font-family: monospace;
      word-break: break-all;
    }
    .ok {
      color: var(--uui-color-positive);
    }
    .warn {
      color: var(--uui-color-warning-standalone);
    }
    .missing {
      color: var(--uui-color-text-alt);
    }
  `;
}

export default DdUrlInspectorElement;
