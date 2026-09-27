import type { ApiError } from '../../api/errors';
import { Button } from './Button';
import { Icon } from './Icon';
import styles from './ErrorState.module.css';

interface ErrorStateProps {
  error: ApiError;
  onRetry?: () => void;
  retrying?: boolean;
  /** "inline" para dentro de cards; "page" ocupa a área de conteúdo. */
  variant?: 'inline' | 'page';
}

/**
 * Falha ao carregar dados. Para problemas de conexão mostra uma mensagem amigável com
 * "Tentar novamente"; o status HTTP e o traceId ficam em "Detalhes técnicos".
 */
export function ErrorState({ error, onRetry, retrying = false, variant = 'inline' }: ErrorStateProps) {
  const title = error.isConnectivity ? 'Não foi possível conectar ao servidor' : 'Não foi possível carregar os dados';
  const description = error.isConnectivity
    ? 'Verifique se a API está em execução e tente novamente em alguns instantes.'
    : error.message;

  return (
    <div className={`${styles.state} ${styles[variant]}`} role="alert">
      <span className={styles.icon}>
        <Icon name={error.isConnectivity ? 'plug' : 'alert'} size={20} />
      </span>
      <p className={styles.title}>{title}</p>
      <p className={styles.description}>{description}</p>
      {onRetry && (
        <Button variant="secondary" icon="refresh" onClick={onRetry} loading={retrying} className={styles.retry}>
          Tentar novamente
        </Button>
      )}
      <details className={styles.details}>
        <summary>Detalhes técnicos</summary>
        <code className="mono">{error.technicalDetails}</code>
      </details>
    </div>
  );
}
