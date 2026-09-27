import type { RouteName } from '../../hooks/useRoute';
import type { IconName } from '../ui/Icon';

export interface NavItem {
  route: RouteName;
  label: string;
  icon: IconName;
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

export const NAV_SECTIONS: NavSection[] = [
  {
    title: 'Visão geral',
    items: [{ route: 'dashboard', label: 'Dashboard', icon: 'dashboard' }],
  },
  {
    title: 'Operações',
    items: [
      { route: 'transfer', label: 'Nova transferência', icon: 'transfer' },
      { route: 'schedule', label: 'Agendar transferência', icon: 'calendar' },
    ],
  },
  {
    title: 'Consultas',
    items: [
      { route: 'lookup', label: 'Consultar transferência', icon: 'search' },
      { route: 'account', label: 'Consultar conta', icon: 'wallet' },
    ],
  },
];

export function findNavItem(route: RouteName): { section: NavSection; item: NavItem } | undefined {
  for (const section of NAV_SECTIONS) {
    const item = section.items.find((candidate) => candidate.route === route);
    if (item) return { section, item };
  }
  return undefined;
}
