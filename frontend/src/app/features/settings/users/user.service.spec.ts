import { of } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { TenantUserService } from './user.service';

describe('TenantUserService', () => {
  const user = {
    id: 12,
    email: 'alex@example.com',
    displayName: 'Alex Example',
    role: 'Member',
    isActive: true,
  };

  it('uses tenant-wide endpoints for list, detail, create, and access updates', () => {
    const calls: unknown[][] = [];
    const api = {
      get: (endpoint: string, companyScoped: boolean) => {
        calls.push(['GET', endpoint, companyScoped]);
        return of(endpoint.endsWith('/12') ? user : [user]);
      },
      post: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['POST', endpoint, body, companyScoped]);
        return of(user);
      },
      put: (endpoint: string, body: unknown, companyScoped: boolean) => {
        calls.push(['PUT', endpoint, body, companyScoped]);
        return of(user);
      },
    } as unknown as ApiService;
    const service = new TenantUserService(api);
    const createPayload = {
      email: user.email,
      displayName: user.displayName,
      temporaryPassword: 'One-time-password',
      role: user.role,
    };
    const accessPayload = { role: user.role, isActive: false };

    service.getAll().subscribe();
    service.getById(user.id).subscribe();
    service.create(createPayload).subscribe();
    service.update(user.id, accessPayload).subscribe();

    expect(calls).toEqual([
      ['GET', '/api/v1/tenant/users', false],
      ['GET', '/api/v1/tenant/users/12', false],
      ['POST', '/api/v1/tenant/users', createPayload, false],
      ['PUT', '/api/v1/tenant/users/12', accessPayload, false],
    ]);
  });

  it('rejects malformed team-member responses', () => {
    const api = ({
      get: () => of([{ ...user, isActive: 'yes' }]),
    }) as unknown as ApiService;
    let receivedError: unknown;

    new TenantUserService(api).getAll().subscribe({ error: error => receivedError = error });

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toContain('invalid record');
  });
});
