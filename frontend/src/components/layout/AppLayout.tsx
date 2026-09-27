import { useEffect, useState, type ReactNode } from 'react';
import type { ApiHealth } from '../../hooks/useApiHealth';
import type { RouteName } from '../../hooks/useRoute';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import styles from './AppLayout.module.css';

interface AppLayoutProps {
  current: RouteName;
  health: ApiHealth;
  onRecheckHealth: () => void;
  children: ReactNode;
}

export function AppLayout({ current, health, onRecheckHealth, children }: AppLayoutProps) {
  const [menuOpen, setMenuOpen] = useState(false);

  // Fecha o menu mobile ao trocar de página e com Esc.
  useEffect(() => setMenuOpen(false), [current]);
  useEffect(() => {
    if (!menuOpen) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false);
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [menuOpen]);

  return (
    <div className={styles.shell}>
      {/* Botão (e não âncora #conteudo), porque o hash da URL é usado pelas rotas. */}
      <button type="button" className={styles.skipLink} onClick={() => document.getElementById('conteudo')?.focus()}>
        Pular para o conteúdo
      </button>
      <Sidebar current={current} open={menuOpen} onNavigate={() => setMenuOpen(false)} />
      {menuOpen && <div className={styles.backdrop} onClick={() => setMenuOpen(false)} aria-hidden="true" />}

      <div className={styles.main}>
        <Topbar
          current={current}
          health={health}
          onRecheckHealth={onRecheckHealth}
          menuOpen={menuOpen}
          onToggleMenu={() => setMenuOpen((open) => !open)}
        />
        <main id="conteudo" className={styles.content} tabIndex={-1}>
          {children}
        </main>
      </div>
    </div>
  );
}
