import { Skeleton } from './Skeleton';

/** Placeholder de formulário enquanto os dados necessários (ex.: contas) carregam. */
export function FormSkeleton({ fields = 3 }: { fields?: number }) {
  return (
    <div role="status" aria-label="Carregando formulário" style={{ display: 'flex', flexDirection: 'column', gap: 22 }}>
      {Array.from({ length: fields }, (_, index) => (
        <div key={index} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          <Skeleton width={120} height={12} />
          <Skeleton height={42} />
        </div>
      ))}
      <Skeleton height={44} />
    </div>
  );
}
