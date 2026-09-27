import type { Account, Transfer } from '../../api/types';
import { routeHref } from '../../hooks/useRoute';
import { formatDateTime, shortId, transferTypeLabel } from '../../lib/format';
import { Icon } from '../ui/Icon';
import { Money } from '../ui/Money';
import { TransferStatusBadge } from '../ui/StatusBadge';
import table from '../ui/DataTable.module.css';
import { accountName } from './accountName';

interface TransferTableProps {
  transfers: Transfer[];
  accounts: Account[];
  /** Quando informado, o valor aparece como entrada (+) ou saída (−) para essa conta. */
  perspectiveAccountId?: number;
}

/** Data que representa a transferência na listagem: a agendada, se houver; senão a de criação. */
function referenceDate(transfer: Transfer): string {
  return transfer.scheduledFor ?? transfer.createdAt;
}

/** Só transferências concluídas movimentaram dinheiro: as demais aparecem sem sinal e esmaecidas. */
function amountDisplay(transfer: Transfer, perspectiveAccountId?: number) {
  const moved = transfer.status === 'Completed';
  const direction: 'in' | 'out' | undefined =
    perspectiveAccountId === undefined || !moved
      ? undefined
      : transfer.destinationAccountId === perspectiveAccountId
        ? 'in'
        : 'out';
  return { moved, direction };
}

export function TransferTable({ transfers, accounts, perspectiveAccountId }: TransferTableProps) {
  return (
    <>
      <TransferRows transfers={transfers} accounts={accounts} perspectiveAccountId={perspectiveAccountId} />
      <TransferList transfers={transfers} accounts={accounts} perspectiveAccountId={perspectiveAccountId} />
    </>
  );
}

function TransferRows({ transfers, accounts, perspectiveAccountId }: TransferTableProps) {
  return (
    <div className={`${table.wrapper} ${table.desktopOnly}`}>
      <table className={table.table}>
        <thead>
          <tr>
            <th scope="col">Status</th>
            <th scope="col">Origem → Destino</th>
            <th scope="col" className={table.numeric}>
              Valor
            </th>
            <th scope="col">Data</th>
            <th scope="col">Tipo</th>
            <th scope="col">Id</th>
          </tr>
        </thead>
        <tbody>
          {transfers.map((transfer) => {
            const { moved, direction } = amountDisplay(transfer, perspectiveAccountId);

            return (
              <tr key={transfer.id}>
                <td>
                  <TransferStatusBadge status={transfer.status} />
                </td>
                <td>
                  <span className={table.flow}>
                    {accountName(accounts, transfer.sourceAccountId)}
                    <Icon name="arrowRight" size={14} />
                    {accountName(accounts, transfer.destinationAccountId)}
                  </span>
                </td>
                <td className={table.numeric}>
                  <Money
                    value={transfer.amount}
                    display={direction ? 'signed' : 'plain'}
                    direction={direction}
                    className={moved ? undefined : table.muted}
                  />
                </td>
                <td className="num">{formatDateTime(referenceDate(transfer))}</td>
                <td className={table.muted}>{transferTypeLabel[transfer.type]}</td>
                <td>
                  <a
                    href={routeHref({ name: 'lookup', param: transfer.id })}
                    className={table.idLink}
                    aria-label={`Ver detalhes da transferência ${transfer.id}`}
                  >
                    {shortId(transfer.id)}
                  </a>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

/** Em telas pequenas a tabela vira lista: valor e contraparte ficam visíveis sem rolagem lateral. */
function TransferList({ transfers, accounts, perspectiveAccountId }: TransferTableProps) {
  return (
    <ul className={`${table.list} ${table.mobileOnly}`}>
      {transfers.map((transfer) => {
        const { moved, direction } = amountDisplay(transfer, perspectiveAccountId);
        return (
          <li key={transfer.id}>
            <a href={routeHref({ name: 'lookup', param: transfer.id })} className={table.listItem}>
              <span className={table.listMain}>
                <span className={table.flow}>
                  {accountName(accounts, transfer.sourceAccountId)}
                  <Icon name="arrowRight" size={14} />
                  {accountName(accounts, transfer.destinationAccountId)}
                </span>
                <Money
                  value={transfer.amount}
                  display={direction ? 'signed' : 'plain'}
                  direction={direction}
                  className={moved ? undefined : table.muted}
                />
              </span>
              <span className={table.listMeta}>
                <TransferStatusBadge status={transfer.status} />
                <span className="num">{formatDateTime(referenceDate(transfer))}</span>
                <span>· {transferTypeLabel[transfer.type]}</span>
              </span>
            </a>
          </li>
        );
      })}
    </ul>
  );
}
