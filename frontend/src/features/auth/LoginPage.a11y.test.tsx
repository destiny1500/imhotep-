import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import axe from 'axe-core';
import { LoginPage } from './LoginPage';

describe('LoginPage accessibility', () => {
  it('has no serious or critical axe violations', async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const { container } = render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/login']}>
          <LoginPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    const results = await axe.run(container, {
      rules: {
        // color-contrast needs a real rendering engine (canvas), not jsdom
        'color-contrast': { enabled: false },
      },
    });

    const seriousOrCritical = results.violations.filter(
      (violation) => violation.impact === 'serious' || violation.impact === 'critical',
    );

    expect(
      seriousOrCritical.map((v) => `${v.id}: ${v.description}`),
    ).toEqual([]);
  });
});
