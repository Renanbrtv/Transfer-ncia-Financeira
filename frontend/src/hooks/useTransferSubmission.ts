import { useCallback, useRef, useState } from 'react';
import { type ApiError, toApiError } from '../api/errors';
import { newIdempotencyKey } from '../api/transfers';
import type { Transfer } from '../api/types';

type SubmissionState =
  | { status: 'idle' }
  | { status: 'submitting' }
  | { status: 'done'; transfer: Transfer }
  | { status: 'error'; error: ApiError };

/**
 * Envio de uma operação de transferência.
 * - A mesma chave de idempotência é reaproveitada enquanto o conteúdo não muda: se a rede falhar e o
 *   usuário tentar de novo, a API devolve a operação original em vez de executar duas vezes.
 * - Uma recusa por regra de negócio (422) também é um resultado: a API devolve a transferência
 *   registrada como Failed, e ela é exibida como comprovante de recusa.
 */
export function useTransferSubmission<TPayload>(send: (payload: TPayload, idempotencyKey: string) => Promise<Transfer>) {
  const [state, setState] = useState<SubmissionState>({ status: 'idle' });
  const key = useRef(newIdempotencyKey());
  const lastPayload = useRef<string | null>(null);

  const submit = useCallback(
    async (payload: TPayload) => {
      const serialized = JSON.stringify(payload);
      if (lastPayload.current !== serialized) {
        key.current = newIdempotencyKey();
        lastPayload.current = serialized;
      }

      setState({ status: 'submitting' });
      try {
        setState({ status: 'done', transfer: await send(payload, key.current) });
      } catch (reason) {
        const error = toApiError(reason);
        setState(
          error.rejectedTransfer ? { status: 'done', transfer: error.rejectedTransfer } : { status: 'error', error },
        );
      }
    },
    [send],
  );

  const reset = useCallback(() => {
    key.current = newIdempotencyKey();
    lastPayload.current = null;
    setState({ status: 'idle' });
  }, []);

  return { state, submit, reset };
}
