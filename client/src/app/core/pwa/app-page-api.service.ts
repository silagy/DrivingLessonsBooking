import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AppPageApiService {
  private readonly http = inject(HttpClient);
  private readonly appPageUrl = '/index.html';

  getAppPage(): Observable<string> {
    return this.http.get(this.appPageUrl, { responseType: 'text' });
  }
}
