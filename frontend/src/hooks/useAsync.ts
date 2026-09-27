import { useCallback, useEffect, useRef, useState } from 'react';
import { type ApiError, toApiError } from '../api/errors';

export interface AsyncState<T> {
  data: T | null;
  error: ApiError | null;
  loading: boolean;
  /** Recarrega mantendo os dados atuais visíveis até a nova resposta chegar. */
  reload: () => void;
}

/**
 * Carrega dados assíncronos com estados de loading/erro e descarta respostas antigas
 * (se as dependências mudarem no meio de uma requisição, só a mais recente vale).
 * Passe `loader = null` para não carregar nada.
 */
export function useAsync<T>(loader: (() => Promise<T>) | null, deps: readonly unknown[]): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(loader !== null);
  const [reloadToken, setReloadToken] = useState(0);
  const requestId = useRef(0);

  useEffect(() => {
    if (!loader) {
      setLoading(false);
      return;
    }

    const current = ++requestId.current;
    setLoading(true);
    setError(null);

    loader()
      .then((result) => {
        if (current === requestId.current) setData(result);
      })
      .catch((reason: unknown) => {
        if (current === requestId.current) setError(toApiError(reason));
      })
      .finally(() => {
        if (current === requestId.current) setLoading(false);
      });
    // O loader muda a cada render; as dependências explícitas controlam quando recarregar.
  }, [...deps, reloadToken]);

  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  return { data, error, loading, reload };
}
