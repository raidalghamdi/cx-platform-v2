// Mirror of the API DTO contracts. Kept hand-written for now — Phase 1 may
// generate from OpenAPI.

export type ComplaintStatus = 'New' | 'InProgress' | 'Resolved' | 'Closed';
export type Priority = 'Low' | 'Normal' | 'High';
export type InboxChannel = 'Email' | 'WhatsApp' | 'Chat';
export type InboxStatus = 'New' | 'Open' | 'Replied' | 'Closed';

export interface UserDto {
  id: number;
  email: string;
  role: string;
  nameEn: string;
  nameAr: string;
  titleEn: string;
  titleAr: string;
  landing: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: UserDto;
  permissions: RolePermissionDto[];
}

export interface RolePermissionDto {
  role: string;
  pageKey: string;
  allowed: boolean;
}

export interface ContactChannelDto {
  key: string;
  value: string;
}

export interface KpiDto {
  key: string;
  nameEn: string;
  nameAr: string;
  value: number;
  unit: string;
  delta: number;
  target?: number;
  source: string;
  lastSyncAt: string;
}

export interface ComplaintListItemDto {
  id: number;
  code: string;
  category: string;
  subjectEn: string;
  subjectAr: string;
  status: ComplaintStatus;
  priority: Priority;
  channel: string;
  downJourney: boolean;
  journeyStageEn?: string | null;
  journeyStageAr?: string | null;
  openedAt: string;
  closedAt?: string | null;
}

export interface ComplaintDto extends ComplaintListItemDto {
  bodyEn: string;
  bodyAr: string;
  customerId?: number | null;
  assignedTo?: number | null;
  monafasahRef?: string | null;
}

export interface ComplaintsByCategoryDto {
  category: string;
  count: number;
}

export interface InboxThreadDto {
  id: number;
  channel: InboxChannel;
  fromAddress: string;
  fromName: string;
  subject?: string | null;
  body: string;
  status: InboxStatus;
  priority: Priority;
  receivedAt: string;
  repliedAt?: string | null;
  replySubject?: string | null;
  replyBody?: string | null;
}

export interface NotificationDto {
  id: number;
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  kind: string;
  createdAt: string;
  readAt?: string | null;
}
