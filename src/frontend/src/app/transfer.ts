import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './api';
import { Account } from './models';

@Component({
  selector: 'app-transfer',
  imports: [FormsModule, DecimalPipe],
  template: `
    <h1>Transfer funds</h1>

    @if (accounts().length === 0) {
      <p>Loading accounts…</p>
    } @else {
      <form class="card" (ngSubmit)="submit()">
        <label>
          From
          <select name="from" [(ngModel)]="fromAccountId" required>
            <option [ngValue]="undefined" disabled>Select an account</option>
            @for (a of accounts(); track a.id) {
              <option [ngValue]="a.id">
                #{{ a.id }} — {{ a.owner }} ({{ a.balance | number: '1.2-2' }})
              </option>
            }
          </select>
        </label>
        <label>
          To
          <select name="to" [(ngModel)]="toAccountId" required>
            <option [ngValue]="undefined" disabled>Select an account</option>
            @for (a of accounts(); track a.id) {
              <option [ngValue]="a.id">#{{ a.id }} — {{ a.owner }}</option>
            }
          </select>
        </label>
        <label>
          Amount
          <input name="amt" type="number" step="0.01" min="0.01" [(ngModel)]="amount" required />
        </label>
        <button type="submit" [disabled]="loading()">
          {{ loading() ? 'Sending…' : 'Send transfer' }}
        </button>
      </form>
    }

    @if (message()) {
      <p [class.error]="isError()" [class.ok]="!isError()">{{ message() }}</p>
    }
  `,
  styles: `
    .card { display: flex; flex-direction: column; gap: 0.8rem; max-width: 360px; }
    label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.9rem; }
    select, input {
      padding: 0.5rem 0.65rem;
      border: 1px solid #ccc;
      border-radius: 6px;
      font: inherit;
    }
    button {
      padding: 0.6rem;
      border: none;
      border-radius: 6px;
      background: #1a56db;
      color: #fff;
      font-weight: 600;
      cursor: pointer;
    }
    button:disabled { opacity: 0.5; cursor: default; }
    .error { color: #b91c1c; }
    .ok { color: #047857; }
  `,
})
export class Transfer implements OnInit {
  private api = inject(ApiService);

  readonly accounts = signal<Account[]>([]);
  fromAccountId?: number;
  toAccountId?: number;
  amount?: number;
  readonly loading = signal(false);
  readonly message = signal('');
  readonly isError = signal(false);

  ngOnInit(): void {
    this.api.getAccounts().subscribe({
      next: (a) => this.accounts.set(a),
      error: () => this.message.set('Could not load accounts'),
    });
  }

  submit(): void {
    if (this.fromAccountId == null || this.toAccountId == null || this.amount == null) {
      return;
    }
    this.loading.set(true);
    this.message.set('');

    this.api
      .transfer({
        fromAccountId: this.fromAccountId,
        toAccountId: this.toAccountId,
        amount: this.amount,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.isError.set(false);
          this.message.set(
            `Transferred ${this.amount} from account ${this.fromAccountId} to ${this.toAccountId}.`,
          );
          this.refreshAccounts();
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.isError.set(true);
          this.message.set(
            typeof err.error === 'string'
              ? err.error
              : (err.error?.message ?? 'Transfer failed'),
          );
        },
      });
  }

  private refreshAccounts(): void {
    this.api.getAccounts().subscribe({ next: (a) => this.accounts.set(a) });
  }
}
