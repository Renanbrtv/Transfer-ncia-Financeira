import { useEffect, useState } from 'react';
import { Icon } from './Icon';
import styles from './CopyButton.module.css';

/** Copia um valor (ex.: Id da transferência) com confirmação visual discreta. */
export function CopyButton({ value, label = 'Copiar' }: { value: string; label?: string }) {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied) return;
    const timer = window.setTimeout(() => setCopied(false), 1800);
    return () => window.clearTimeout(timer);
  }, [copied]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
    } catch {
      // Clipboard indisponível (contexto não seguro): o valor continua selecionável na tela.
    }
  };

  return (
    <button type="button" className={styles.copy} onClick={() => void copy()} aria-label={`${label}: ${value}`}>
      <Icon name={copied ? 'check' : 'copy'} size={14} />
      <span>{copied ? 'Copiado' : label}</span>
    </button>
  );
}
