const SERVICE_WORKER_URL = '/sw.js';
const SERVICE_WORKER_SCOPE = '/';

export async function registerServiceWorker(container: ServiceWorkerContainer | undefined): Promise<void> {
  if (!container) {
    return;
  }

  try {
    await container.register(SERVICE_WORKER_URL, { scope: SERVICE_WORKER_SCOPE });
  } catch {
    return;
  }
}
