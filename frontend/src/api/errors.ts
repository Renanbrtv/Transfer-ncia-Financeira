import type { ProblemDetails, Transfer } from './types';

export type ApiErrorKind =
  | 'network' // sem resposta: servidor fora do ar, DNS, CORS, timeout
  | 'unavailable' // 502/503/504: proxy de pé, mas a API não respondeu
  | 'validation' // 400
  | 'notFound' // 404
  | 'conflict' // 409
  | 'rejected' // 422: regra de negócio recusou (a transferência fica registrada como Failed)
  | 'server' // 500
  | 'unknown';

const CONNECTION_MESSAGE = 'Não foi possível conectar ao servidor.';

/**
 * Erro de API já normalizado. `message` é sempre adequada para o usuário;
 * detalhes técnicos (status, código, traceId) ficam em `technicalDetails`.
 */
export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(kind: ApiErrorKind, status: number, problem: ProblemDetails, message: string) {
    super(message);
    this.name = 'ApiError';
    this.kind = kind;
    this.status = status;
    this.problem = problem;
  }

  /** Falha de conectividade: vale oferecer "Tentar novamente". */
  get isConnectivity(): boolean {
    return this.kind === 'network' || this.kind === 'unavailable';
  }

  /** Transferência registrada como Failed, devolvida junto com o 422. */
  get rejectedTransfer(): Transfer | undefined {
    return this.problem.transfer;
  }

  get technicalDetails(): string {
    const parts = [
      this.status > 0 ? `HTTP ${this.status}` : 'sem resposta do servidor',
      this.problem.code ? `código ${this.problem.code}` : null,
      this.problem.traceId ? `trace ${this.problem.traceId}` : null,
    ];
    return parts.filter(Boolean).join(' · ');
  }

  static network(reason: 'offline' | 'timeout'): ApiError {
    const message =
      reason === 'timeout' ? 'O servidor demorou para responder. Tente novamente em instantes.' : CONNECTION_MESSAGE;
    return new ApiError('network', 0, {}, message);
  }

  static fromResponse(status: number, problem: ProblemDetails): ApiError {
    const kind = kindFromStatus(status);
    return new ApiError(kind, status, problem, userMessage(kind, problem));
  }
}

function kindFromStatus(status: number): ApiErrorKind {
  if (status === 502 || status === 503 || status === 504) return 'unavailable';
  if (status === 400) return 'validation';
  if (status === 404) return 'notFound';
  if (status === 409) return 'conflict';
  if (status === 422) return 'rejected';
  if (status >= 500) return 'server';
  return 'unknown';
}

function userMessage(kind: ApiErrorKind, problem: ProblemDetails): string {
  switch (kind) {
    case 'unavailable':
      return CONNECTION_MESSAGE;
    case 'server':
      return 'Ocorreu um erro no servidor. Tente novamente em instantes.';
    case 'validation': {
      const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : [];
      return fieldErrors.length > 0 ? fieldErrors.join(' ') : (problem.detail ?? 'Dados inválidos.');
    }
    default:
      return problem.detail ?? problem.title ?? 'Não foi possível concluir a operação.';
  }
}

/** Converte qualquer erro em ApiError, para as telas tratarem um único tipo. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  return new ApiError('unknown', 0, {}, 'Não foi possível concluir a operação.');
}
