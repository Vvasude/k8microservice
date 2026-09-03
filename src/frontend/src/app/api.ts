import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Account, Transaction, TransferRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  getAccounts(): Observable<Account[]> {
    return this.http.get<Account[]>('/accounts');
  }

  getTransfers(): Observable<Transaction[]> {
    return this.http.get<Transaction[]>('/transfers');
  }

  transfer(body: TransferRequest): Observable<unknown> {
    return this.http.post('/transfers', body);
  }
}
