import type { ApiHealth } from '../../hooks/useApiHealth';
import type { RouteName } from '../../hooks/useRoute';
import { Icon } from '../ui/Icon';
import { Logo } from './Logo';
import { findNavItem } from './navigation';
import styles from './Topbar.module.css';

interface TopbarProps {
  current: RouteName;
  health: ApiHealth;
  onRecheckHealth: () => void;
  menuOpen: boolean;
  onToggleMenu: () => void;
}

const HEALTH_LABEL: Record<ApiHealth, string> = {
  checking: 'Verificando conexão',
  online: 'API conectada',
  offline: 'API indisponível',
};

export function Topbar({ current, health, onRecheckHealth, menuOpen, onToggleMenu }: TopbarProps) {
  const location = findNavItem(current);

  return (
    <header className={styles.topbar}>
      <button
        type="button"
        className={styles.menuButton}
        onClick={onToggleMenu}
        aria-expanded={menuOpen}
        aria-controls="app-sidebar"
        aria-label={menuOpen ? 'Fechar menu' : 'Abrir menu'}
      >
        <Icon name={menuOpen ? 'close' : 'menu'} size={20} />
      </button>

      <div className={styles.mobileLogo}>
        <Logo compact />
      </div>

      {location && (
        <nav className={styles.breadcrumb} aria-label="Você está em">
          <span>{location.section.title}</span>
          <Icon name="chevronRight" size={14} />
          <span className={styles.breadcrumbCurrent}>{location.item.label}</span>
        </nav>
      )}

      <div className={styles.right}>
        <span className={styles.environment}>Ambiente de demonstração</span>
        <button
          type="button"
          className={`${styles.health} ${styles[health]}`}
          onClick={onRecheckHealth}
          title="Verificar conexão com a API"
          aria-live="polite"
        >
          <span className={styles.healthDot} aria-hidden="true" />
          <span className={styles.healthLabel}>{HEALTH_LABEL[health]}</span>
        </button>
      </div>
    </header>
  );
}
