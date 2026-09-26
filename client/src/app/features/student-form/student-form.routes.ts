import { Routes } from '@angular/router';
import { StudentFormPage } from './ui/pages/student-form/student-form.page';

export default [{ path: ':token', component: StudentFormPage }] satisfies Routes;
