import { AppRoutes } from '../../shared/config/app-routes';

const STUDENT_FORM_PATH = `/${AppRoutes.studentForm}/`;

export function holdBackInstallPromptOnStudentForm(target: Window): void {
  target.addEventListener('beforeinstallprompt', (event) => {
    if (target.location.pathname.startsWith(STUDENT_FORM_PATH)) {
      event.preventDefault();
    }
  });
}
