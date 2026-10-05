export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'workspaceView',
    alias: 'DynamicDave.UrlInspector.WorkspaceView',
    name: 'URL Inspector Workspace View',
    element: () => import('./url-inspector.element.js'),
    weight: 150,
    meta: { label: '#ddUrlInspector_tab', pathname: 'urls-and-redirects', icon: 'icon-link' },
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', match: 'Umb.Workspace.Document' }],
  },
  {
    type: 'localization',
    alias: 'DynamicDave.UrlInspector.Localization.En',
    name: 'URL Inspector English',
    weight: -100,
    meta: { culture: 'en' },
    js: () => import('./localization/en.js'),
  },
  {
    type: 'localization',
    alias: 'DynamicDave.UrlInspector.Localization.Nl',
    name: 'URL Inspector Dutch',
    weight: -100,
    meta: { culture: 'nl' },
    js: () => import('./localization/nl.js'),
  },
];
