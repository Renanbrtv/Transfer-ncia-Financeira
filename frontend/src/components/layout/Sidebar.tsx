import { routeHref, type RouteName } from '../../hooks/useRoute';
import { Icon } from '../ui/Icon';
import { Logo } from './Logo';
import { NAV_SECTIONS } from './navigation';
import styles from './Sidebar.module.css';

interface SidebarProps {
  current: RouteName;
  open: boolean;
  onNavigate: () => void;
}

export function Sidebar({ current, open, onNavigate }: SidebarProps) {
  return (
    <aside id="app-sidebar" className={`${styles.sidebar} ${open ? styles.open : ''}`} aria-label="Menu principal">
      <div className={styles.brand}>
        <a href={routeHref({ name: 'dashboard' })} onClick={onNavigate} aria-label="TransferFlow, ir para o dashboard">
          <Logo />
        </a>
      </div>

      <nav className={styles.nav}>
        {NAV_SECTIONS.map((section) => (
          <div key={section.title} className={styles.section}>
            <p className={styles.sectionTitle}>{section.title}</p>
            <ul>
              {section.items.map((item) => {
                const active = item.route === current;
                return (
                  <li key={item.route}>
                    <a
                      href={routeHref({ name: item.route })}
                      className={`${styles.link} ${active ? styles.active : ''}`}
                      aria-current={active ? 'page' : undefined}
                      onClick={onNavigate}
                    >
                      <Icon name={item.icon} size={18} />
                      {item.label}
                    </a>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <footer className={styles.footer}>
        <p className={styles.footerTitle}>Sistema demonstrativo</p>
        <p>Teste técnico · v1.0</p>
      </footer>
    </aside>
  );
}
