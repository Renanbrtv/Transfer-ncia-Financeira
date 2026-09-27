// Contratos da API (espelham os DTOs do backend).

export type AccountStatus = 'Active' | 'Blocked' | 'Inactive';

export type TransferStatus = 'Scheduled' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';

export type TransferType = 'Immediate' | 'Scheduled';

export interface Account {
  id: number;
  holderName: string;
  balance: number;
  overdraftLimit: number;
  availableBalance: number;
  status: AccountStatus;
}

export interface Transfer {
  id: string;
  sourceAccountId: number;
  destinationAccountId: number;
  amount: number;
  type: TransferType;
  status: TransferStatus;
  createdAt: string;
  scheduledFor: string | null;
  processedAt: string | null;
  cancelledAt: string | null;
  failureCode: string | null;
  failureMessage: string | null;
}

export interface CreateTransferRequest {
  sourceAccountId: number;
  destinationAccountId: number;
  amount: number;
}

export interface ScheduleTransferRequest extends CreateTransferRequest {
  scheduledFor: string;
}

/** RFC 7807, com as extensões que a API adiciona. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  traceId?: string;
  transferId?: string;
  transfer?: Transfer;
  errors?: Record<string, string[]>;
}
