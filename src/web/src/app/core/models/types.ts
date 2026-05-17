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

// ── Phase 1 ───────────────────────────────────────────────────────────────

export type JourneyStatus = 'active' | 'draft' | 'retired';

export interface JourneyDto {
  id: number;
  nameEn: string;
  nameAr: string;
  persona: string;
  stageCount: number;
  status: JourneyStatus;
  createdAt: string;
}

export interface JourneyStageDto {
  id: number;
  journeyId: number;
  sequence: number;
  nameEn: string;
  nameAr: string;
  touchpointEn: string;
  touchpointAr: string;
  painPointEn: string;
  painPointAr: string;
  emotionScore: number;          // -2..+2
}

export interface JourneyDetailDto {
  journey: JourneyDto;
  stages: JourneyStageDto[];
}

export type Sentiment = 'positive' | 'neutral' | 'negative';

export interface VocResponseDto {
  id: number;
  surveyEn: string;
  surveyAr: string;
  channel: string;
  npsScore: number;
  sentiment: Sentiment;
  commentEn: string;
  commentAr: string;
  respondedAt: string;
  customerName: string;
}

export type KbStatus = 'draft' | 'published' | 'retired';

export interface KbArticleDto {
  id: number;
  titleEn: string;
  titleAr: string;
  category: string;
  bodyEn: string;
  bodyAr: string;
  authorId?: number | null;
  status: KbStatus;
  updatedAt: string;
}

export interface UpsertKbArticleRequest {
  titleEn: string;
  titleAr: string;
  category: string;
  bodyEn: string;
  bodyAr: string;
  status: KbStatus;
}

export type RagStatus = 'red' | 'amber' | 'green';

export interface ProgrammeInitiativeDto {
  id: number;
  nameEn: string;
  nameAr: string;
  owner: string;
  ragStatus: RagStatus;
  progressPct: number;
  startDate: string;
  targetDate: string;
  notes: string;
}

export interface UpdateProgrammeStatusRequest {
  ragStatus: RagStatus;
  progressPct: number;
  notes?: string;
}

export interface GovernanceBodyDto {
  id: number;
  nameEn: string;
  nameAr: string;
  cadence: string;
  chair: string;
  members: string[];
  charterUrl?: string | null;
}

export interface GovernanceDecisionDto {
  id: number;
  bodyId: number;
  decidedAt: string;
  titleEn: string;
  titleAr: string;
  decision: string;
  ownerEn: string;
  ownerAr: string;
  dueDate?: string | null;
}

export interface GovernanceBodyDetailDto {
  body: GovernanceBodyDto;
  decisions: GovernanceDecisionDto[];
}

export interface CreateGovernanceDecisionRequest {
  titleEn: string;
  titleAr: string;
  decision: string;
  ownerEn: string;
  ownerAr: string;
  dueDate?: string | null;
}

// ── Phase 2 ───────────────────────────────────────────────────────────────

export interface AboutSectionDto {
  id: number;
  keyEn: string;
  keyAr: string;
  bodyEn: string;
  bodyAr: string;
  orderIndex: number;
  updatedAt: string;
}

export interface UpdateAboutSectionRequest {
  keyEn: string;
  keyAr: string;
  bodyEn: string;
  bodyAr: string;
  orderIndex: number;
}

export interface ArchitectureDomainDto {
  id: string;
  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;
}

export interface ArchitecturePatternDto {
  id: string;
  nameEn: string;
  nameAr: string;
  style: 'synchronous' | 'asynchronous' | 'batch';
  usageEn: string;
  usageAr: string;
}

export interface ArchitectureReferenceDto {
  domains: ArchitectureDomainDto[];
  patterns: ArchitecturePatternDto[];
}

export type PortalRequestType = 'complaint' | 'inquiry' | 'appointment';
export type PortalStatus = 'new' | 'in_progress' | 'resolved' | 'closed';

export interface PortalRequestDto {
  id: number;
  type: PortalRequestType;
  subjectEn: string;
  subjectAr: string;
  bodyEn: string;
  bodyAr: string;
  status: PortalStatus;
  createdAt: string;
}

export interface CreatePortalRequestRequest {
  type: PortalRequestType;
  subjectEn: string;
  subjectAr: string;
  bodyEn: string;
  bodyAr: string;
}

export type CopilotIntent = 'ask' | 'draft_reply' | 'summarise' | 'find_similar';

export interface AskCopilotRequest {
  intent: CopilotIntent;
  promptEn: string;
  promptAr: string;
}

export interface CopilotInteractionDto {
  id: number;
  intent: CopilotIntent;
  promptEn: string;
  promptAr: string;
  responseEn: string;
  responseAr: string;
  latencyMs: number;
  success: boolean;
  createdAt: string;
}

export interface AuditEventDto {
  id: number;
  kind: string;
  actorUserId?: number | null;
  targetKind: string;
  targetId?: number | null;
  prevHash: string;
  entryHash: string;
  payloadJson: string;
  at: string;
}

export interface AuditPageDto {
  items: AuditEventDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AuditVerifyResultDto {
  ok: boolean;
  total: number;
  firstBrokenIndex?: number | null;
  firstBrokenId?: number | null;
}

export interface AutomationRuleDto {
  id: number;
  nameEn: string;
  nameAr: string;
  triggerType: string;
  conditionJson: string;
  actionType: string;
  enabled: boolean;
  lastRunAt?: string | null;
  lastRunStatus: string;
  runCount: number;
}

export interface AutomationRunResultDto {
  ok: boolean;
  status: string;
  latencyMs: number;
  note?: string | null;
}
