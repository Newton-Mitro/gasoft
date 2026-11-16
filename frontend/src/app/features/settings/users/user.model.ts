export interface TenantUser {
  id: number;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
}

export interface CreateTenantUserPayload {
  email: string;
  displayName: string;
  temporaryPassword: string;
  role: string;
}

export interface AddExistingTenantUserPayload {
  email: string;
  role: string;
}

export interface UpdateTenantUserPayload {
  role: string;
  isActive: boolean;
}
