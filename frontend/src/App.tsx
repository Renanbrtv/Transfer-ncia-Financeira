import { AppLayout } from './components/layout/AppLayout';
import { useApiHealth } from './hooks/useApiHealth';
import { useRoute, type Route } from './hooks/useRoute';
import { AccountPage } from './pages/AccountPage';
import { DashboardPage } from './pages/DashboardPage';
import { NewTransferPage } from './pages/NewTransferPage';
import { ScheduleTransferPage } from './pages/ScheduleTransferPage';
import { TransferLookupPage } from './pages/TransferLookupPage';

export default function App() {
  const [route, navigate] = useRoute();
  const { health, recheck } = useApiHealth();

  return (
    <AppLayout current={route.name} health={health} onRecheckHealth={recheck}>
      <Page route={route} navigate={navigate} />
    </AppLayout>
  );
}

function Page({ route, navigate }: { route: Route; navigate: (route: Route) => void }) {
  const goToNewTransfer = () => navigate({ name: 'transfer' });

  switch (route.name) {
    case 'transfer':
      return <NewTransferPage />;
    case 'schedule':
      return <ScheduleTransferPage />;
    case 'lookup':
      return (
        <TransferLookupPage transferId={route.param} onSearch={(id) => navigate({ name: 'lookup', param: id })} />
      );
    case 'account':
      return (
        <AccountPage
          accountId={route.param}
          onSelect={(id) => navigate({ name: 'account', param: id })}
          onNewTransfer={goToNewTransfer}
        />
      );
    default:
      return <DashboardPage onNewTransfer={goToNewTransfer} />;
  }
}
