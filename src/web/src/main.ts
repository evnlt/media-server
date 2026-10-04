import './style.css';
import { getCurrentUser } from './api/auth';
import { renderHome } from './pages/home';
import { renderLogin } from './pages/login';

const root = document.querySelector<HTMLElement>('#app')!;

async function start(): Promise<void> {
  try {
    const user = await getCurrentUser();
    if (user) renderHome(root, user, start);
    else renderLogin(root, start);
  } catch {
    root.textContent = 'Cannot reach the server.';
  }
}

start();
