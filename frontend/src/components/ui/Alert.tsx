import type { ReactNode } from 'react';
import { Icon, type IconName } from './Icon';
import styles from './Alert.module.css';

type Tone = 'info' | 'success' | 'warning' | 'error';

const ICONS: Record<Tone, IconName> = {
  info: 'info',
  success: 'check',
  warning: 'alert',
  error: 'alert',
};

interface AlertProps {
  tone: Tone;
  title?: string;
  children?: ReactNode;
  action?: ReactNode;
}

export function Alert({ tone, title, children, action }: AlertProps) {
  return (
    <div className={`${styles.alert} ${styles[tone]}`} role={tone === 'error' ? 'alert' : 'status'}>
      <Icon name={ICONS[tone]} size={18} className={styles.icon} />
      <div className={styles.content}>
        {title && <p className={styles.title}>{title}</p>}
        {children && <div className={styles.body}>{children}</div>}
      </div>
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
