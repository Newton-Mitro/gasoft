export type JournalType = 'General' | 'Sales' | 'Purchase' | 'Bank';

export interface Journal {
  id: number;
  code: string;
  name: string;
  journalType: JournalType;
  isActive: boolean;
}

export interface JournalPayload {
  code: string;
  name: string;
  journalType: JournalType;
  isActive?: boolean;
}
