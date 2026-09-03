import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from './auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="card">
      <h1>{{ mode() === 'login' ? 'Log in' : 'Register' }}</h1>

      <form (ngSubmit)="submit()">
        <input
          name="username"
          [(ngModel)]="username"
          placeholder="Username"
          autocomplete="username"
          required
        />
        <input
          name="password"
          type="password"
          [(ngModel)]="password"
          placeholder="Password"
          autocomplete="{{ mode() === 'login' ? 'current-password' : 'new-password' }}"
          required
        />
        <button type="submit" [disabled]="loading() || !username || !password">
          {{ loading() ? '…' : mode() === 'login' ? 'Log in' : 'Create account' }}
        </button>
      </form>

      @if (message()) {
        <p [class.error]="isError()">{{ message() }}</p>
      }

      <button type="button" class="link" (click)="toggle()">
        {{ mode() === 'login' ? 'Need an account? Register' : 'Have an account? Log in' }}
      </button>
    </div>
  `,
  styles: `
    .card {
      max-width: 320px;
      margin: 3rem auto;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    form { display: flex; flex-direction: column; gap: 0.6rem; }
    input {
      padding: 0.55rem 0.7rem;
      border: 1px solid #ccc;
      border-radius: 6px;
      font: inherit;
    }
    button[type='submit'] {
      padding: 0.6rem;
      border: none;
      border-radius: 6px;
      background: #1a56db;
      color: #fff;
      font-weight: 600;
      cursor: pointer;
    }
    button[type='submit']:disabled { opacity: 0.5; cursor: default; }
    .link {
      background: none;
      border: none;
      color: #1a56db;
      cursor: pointer;
      font: inherit;
    }
    .error { color: #b91c1c; }
  `,
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  readonly mode = signal<'login' | 'register'>('login');
  username = '';
  password = '';
  readonly message = signal('');
  readonly isError = signal(false);
  readonly loading = signal(false);

  toggle(): void {
    this.mode.set(this.mode() === 'login' ? 'register' : 'login');
    this.message.set('');
  }

  submit(): void {
    this.loading.set(true);
    this.message.set('');
    const op =
      this.mode() === 'login'
        ? this.auth.login(this.username, this.password)
        : this.auth.register(this.username, this.password);

    op.subscribe({
      next: () => {
        this.loading.set(false);
        if (this.mode() === 'register') {
          this.mode.set('login');
          this.isError.set(false);
          this.message.set('Registered — now log in.');
        } else {
          this.router.navigate(['/accounts']);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.isError.set(true);
        this.message.set(
          err.status === 401
            ? 'Invalid credentials'
            : err.status === 409
              ? 'Username already taken'
              : 'Something went wrong',
        );
      },
    });
  }
}
