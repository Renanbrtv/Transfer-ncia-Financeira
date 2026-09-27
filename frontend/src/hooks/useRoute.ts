import { useCallback, useEffect, useState } from 'react';

export type RouteName = 'dashboard' | 'transfer' | 'schedule' | 'lookup' | 'account';

export interface Route {
  name: RouteName;
  /** Parâmetro opcional: Id da transferência (lookup) ou da conta (account). */
  param?: string;
}

const PATHS: Record<RouteName, string> = {
  dashboard: 'dashboard',
  transfer: 'transferir',
  schedule: 'agendar',
  lookup: 'transferencias',
  account: 'contas',
};

const NAMES = Object.fromEntries(Object.entries(PATHS).map(([name, path]) => [path, name])) as Record<string, RouteName>;

export function routeHref(route: Route): string {
  return `#/${PATHS[route.name]}${route.param ? `/${encodeURIComponent(route.param)}` : ''}`;
}

function parseHash(hash: string): Route {
  const [path = '', param] = hash.replace(/^#\/?/, '').split('/');
  const name = NAMES[path] ?? 'dashboard';
  return { name, param: param ? decodeURIComponent(param) : undefined };
}

/**
 * Roteamento por hash (#/transferir, #/transferencias/{id}). Funciona com voltar/avançar do navegador
 * e com links diretos, sem exigir configuração no servidor nem dependência extra.
 */
export function useRoute(): [Route, (route: Route) => void] {
  const [route, setRoute] = useState<Route>(() => parseHash(window.location.hash));

  useEffect(() => {
    const onHashChange = () => setRoute(parseHash(window.location.hash));
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const navigate = useCallback((next: Route) => {
    window.location.hash = routeHref(next);
  }, []);

  return [route, navigate];
}
