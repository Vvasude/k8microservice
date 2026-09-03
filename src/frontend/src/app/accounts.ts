import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ApiService } from './api';
import { Account } from './models';

@Component({
  selector: 'app-accounts',
  imports: [DecimalPipe],
  template: `
    <h1>Accounts</h1>

    @if (loading()) {
      <p>Loading…</p>
    } @else if (error()) {
      <p class="error">{{ error() }}</p>
    } @else {
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Owner</th>
            <th class="num">Balance</th>
          </tr>
        </thead>
        <tbody>
          @for (a of accounts(); track a.id) {
            <tr>
              <td>{{ a.id }}</td>
              <td>{{ a.owner }}</td>
              <td class="num">{{ a.balance | number: '1.2-2' }}</td>
            </tr>
          }
        </tbody>
      </table>
      <p class="hint">
        These accounts are shared demo data — use their IDs on the Transfer page.
      </p>
    }
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.55rem 0.6rem; border-bottom: 1px solid #e2e2e6; }
    .num { text-align: right; font-variant-numeric: tabular-nums; }
    .error { color: #b91c1c; }
    .hint { color: #666; font-size: 0.85rem; margin-top: 0.75rem; }
  `,
})
export class Accounts implements OnInit {
  private api = inject(ApiService);

  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.getAccounts().subscribe({
      next: (a) => {
        this.accounts.set(a);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load accounts');
        this.loading.set(false);
      },
    });
  }
}
