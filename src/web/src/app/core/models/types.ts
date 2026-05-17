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

// ── Round 5 — maturity-model types ─────────────────────────────────────────

export type WcagLevel = 'AA' | 'AAA';
export type AccessibilitySeverity = 'Low' | 'Medium' | 'High' | 'Critical';
export type AccessibilityItemStatus = 'Open' | 'InProgress' | 'Resolved' | 'Deferred';

export interface AccessibilityAuditDto {
  id: number;
  auditDate: string;
  auditor: string;
  scopePages: string[];
  wcagLevel: WcagLevel;
  totalIssues: number;
  openIssues: number;
  reportUrl?: string | null;
  notes: string;
}

export interface AccessibilityRemediationDto {
  id: number;
  auditId: number;
  wcagCriterion: string;
  severity: AccessibilitySeverity;
  descriptionEn: string;
  descriptionAr: string;
  owner: string;
  status: AccessibilityItemStatus;
  targetDate?: string | null;
  resolvedDate?: string | null;
}

export interface AccessibilityAuditDetailDto {
  audit: AccessibilityAuditDto;
  items: AccessibilityRemediationDto[];
}

export interface WcagCriterionDto {
  id: string;
  name: string;
  level: string;
  principle: string;
}

export interface UpdateAccessibilityRemediationRequest {
  status: AccessibilityItemStatus;
  targetDate?: string | null;
  owner: string;
}

export type IncidentSeverity = 'Sev1' | 'Sev2' | 'Sev3' | 'Sev4';
export type IncidentStatus = 'Open' | 'Mitigating' | 'Resolved';
export type CheckStatus = 'Pass' | 'Fail';

export interface ServiceHealthMetricDto {
  id: number;
  serviceName: string;
  measuredAt: string;
  uptimePct: number;
  p95LatencyMs: number;
  errorRatePct: number;
  mttrMinutes: number;
  requestCount: number;
}

export interface ServiceIncidentDto {
  id: number;
  serviceName: string;
  openedAt: string;
  resolvedAt?: string | null;
  severity: IncidentSeverity;
  titleEn: string;
  titleAr: string;
  rootCauseEn: string;
  rootCauseAr: string;
  remediationEn: string;
  remediationAr: string;
  status: IncidentStatus;
}

export interface SyntheticCheckDto {
  id: number;
  name: string;
  endpoint: string;
  intervalSeconds: number;
  lastRunAt?: string | null;
  lastStatus: CheckStatus;
  lastLatencyMs: number;
  enabled: boolean;
}

export type PdcaStage = 'Plan' | 'Do' | 'Check' | 'Act' | 'Closed';
export type ImprovementSource = 'KpiBreach' | 'AccessibilityAudit' | 'ContentReview' | 'Manual';
export type ImprovementPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type ThresholdComparison = 'LessThan' | 'GreaterThan';
export type ThresholdBreachAction = 'CreateImprovementItem' | 'NotifyOnly' | 'Both';

export interface KpiThresholdDto {
  id: number;
  kpiId: number;
  kpiKey: string;
  thresholdValue: number;
  comparisonOp: ThresholdComparison;
  breachAction: ThresholdBreachAction;
  enabled: boolean;
}

export interface UpdateKpiThresholdRequest {
  thresholdValue: number;
  comparisonOp: ThresholdComparison;
  breachAction: ThresholdBreachAction;
  enabled: boolean;
}

export interface ImprovementItemDto {
  id: number;
  sourceType: ImprovementSource;
  sourceRefId?: number | null;
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  owner: string;
  priority: ImprovementPriority;
  pdcaStage: PdcaStage;
  createdAt: string;
  targetDate?: string | null;
  closedAt?: string | null;
}

export interface PdcaLogDto {
  id: number;
  improvementItemId: number;
  fromStage: PdcaStage;
  toStage: PdcaStage;
  actorUserId?: number | null;
  changedAt: string;
  notesEn: string;
  notesAr: string;
}

export interface ImprovementItemDetailDto {
  item: ImprovementItemDto;
  log: PdcaLogDto[];
}

export interface CreateImprovementItemRequest {
  sourceType: ImprovementSource;
  sourceRefId?: number | null;
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  owner: string;
  priority: ImprovementPriority;
  targetDate?: string | null;
}

export interface TransitionPdcaRequest {
  toStage: PdcaStage;
  notesEn: string;
  notesAr: string;
}

export type CxSegment = 'All' | 'NewCustomer' | 'Returning' | 'VIP';

export interface CxAnalyticsSnapshotDto {
  id: number;
  snapshotDate: string;
  csat: number;
  nps: number;
  ces: number;
  complaintVolume: number;
  resolutionRateP95Hours: number;
  journeyId?: number | null;
  segment: CxSegment;
}

export interface CxAnalyticsTrendDto {
  segment: CxSegment;
  points: CxAnalyticsSnapshotDto[];
}

export interface RootCauseLinkDto {
  id: number;
  fromType: string;
  fromRefId: number;
  toType: string;
  toRefId: number;
  linkStrength: number;
  notes: string;
}

export interface CreateRootCauseLinkRequest {
  fromType: string;
  fromRefId: number;
  toType: string;
  toRefId: number;
  linkStrength: number;
  notes: string;
}

export type ContentReviewStatus = 'Pending' | 'InReview' | 'Approved' | 'Rejected';

export interface ContentReviewCycleDto {
  id: number;
  kbArticleId: number;
  articleTitleEn: string;
  articleTitleAr: string;
  dueDate: string;
  assignedReviewer: string;
  status: ContentReviewStatus;
  completedAt?: string | null;
  freshnessScore: number;
  enArParityFlag: boolean;
  notes: string;
}

export interface CreateContentReviewCycleRequest {
  kbArticleId: number;
  dueDate: string;
  assignedReviewer: string;
}

export interface UpdateContentReviewCycleRequest {
  status: ContentReviewStatus;
  assignedReviewer: string;
  notes: string;
}

export interface ChannelPerformanceMetricDto {
  id: number;
  channel: string;
  measuredAt: string;
  volumeCount: number;
  avgResponseMinutes: number;
  resolutionRatePct: number;
  csatScore: number;
}

export interface StaleArticleDto {
  articleId: number;
  titleEn: string;
  titleAr: string;
  category: string;
  updatedAt: string;
  freshnessScore: number;
  enArParityFlag: boolean;
}
