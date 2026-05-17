import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  ComplaintDto, ComplaintListItemDto, ComplaintsByCategoryDto, ContactChannelDto,
  InboxThreadDto, KpiDto, NotificationDto, RolePermissionDto,
  JourneyDto, JourneyDetailDto, VocResponseDto, KbArticleDto, UpsertKbArticleRequest,
  ProgrammeInitiativeDto, UpdateProgrammeStatusRequest, GovernanceBodyDto,
  GovernanceBodyDetailDto, GovernanceDecisionDto, CreateGovernanceDecisionRequest,
  AboutSectionDto, UpdateAboutSectionRequest, ArchitectureReferenceDto,
  PortalRequestDto, CreatePortalRequestRequest,
  AskCopilotRequest, CopilotInteractionDto,
  AuditPageDto, AuditVerifyResultDto,
  AutomationRuleDto, AutomationRunResultDto,
  // Round 5 — maturity model
  AccessibilityAuditDto, AccessibilityAuditDetailDto, AccessibilityRemediationDto,
  WcagCriterionDto, UpdateAccessibilityRemediationRequest,
  ServiceHealthMetricDto, ServiceIncidentDto, SyntheticCheckDto,
  ImprovementItemDto, ImprovementItemDetailDto, CreateImprovementItemRequest, TransitionPdcaRequest,
  KpiThresholdDto, UpdateKpiThresholdRequest,
  CxAnalyticsSnapshotDto, CxAnalyticsTrendDto, RootCauseLinkDto, CreateRootCauseLinkRequest,
  ContentReviewCycleDto, CreateContentReviewCycleRequest, UpdateContentReviewCycleRequest,
  ChannelPerformanceMetricDto, StaleArticleDto,
} from '../models/types';
import { environment } from '../../../environments/environment';

// Thin HTTP wrapper — every endpoint we hit lives here so feature components
// stay focused on rendering rather than URL plumbing.

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiBase;
  constructor(private http: HttpClient) {}

  kpis() {
    return firstValueFrom(this.http.get<KpiDto[]>(`${this.base}/kpis`));
  }

  complaints(filter?: { downJourney?: boolean; status?: string }) {
    const params: any = {};
    if (filter?.downJourney !== undefined) params.downJourney = filter.downJourney;
    if (filter?.status) params.status = filter.status;
    return firstValueFrom(this.http.get<ComplaintListItemDto[]>(`${this.base}/complaints`, { params }));
  }

  complaintsByCategory() {
    return firstValueFrom(this.http.get<ComplaintsByCategoryDto[]>(`${this.base}/complaints/by-category`));
  }

  complaintGet(id: number) {
    return firstValueFrom(this.http.get<ComplaintDto>(`${this.base}/complaints/${id}`));
  }

  complaintStatus(id: number, status: string) {
    return firstValueFrom(this.http.patch<ComplaintDto>(`${this.base}/complaints/${id}/status`, { status }));
  }

  complaintNote(id: number, note: string) {
    return firstValueFrom(this.http.post(`${this.base}/complaints/${id}/notes`, { note }));
  }

  threads(filter?: { channel?: string; status?: string }) {
    const params: any = {};
    if (filter?.channel) params.channel = filter.channel;
    if (filter?.status) params.status = filter.status;
    return firstValueFrom(this.http.get<InboxThreadDto[]>(`${this.base}/inbox/threads`, { params }));
  }

  threadGet(id: number) {
    return firstValueFrom(this.http.get<InboxThreadDto>(`${this.base}/inbox/threads/${id}`));
  }

  threadReply(id: number, body: string, subject?: string) {
    return firstValueFrom(this.http.post<InboxThreadDto>(`${this.base}/inbox/threads/${id}/reply`, { body, subject }));
  }

  threadStatus(id: number, status: string) {
    return firstValueFrom(this.http.patch<InboxThreadDto>(`${this.base}/inbox/threads/${id}/status`, { status }));
  }

  rolePerms() {
    return firstValueFrom(this.http.get<RolePermissionDto[]>(`${this.base}/admin/role-permissions`));
  }

  saveRolePerms(items: RolePermissionDto[]) {
    return firstValueFrom(this.http.patch(`${this.base}/admin/role-permissions`, { items }));
  }

  contactChannels() {
    return firstValueFrom(this.http.get<ContactChannelDto[]>(`${this.base}/admin/contact-channels`));
  }

  saveChannel(key: string, value: string) {
    return firstValueFrom(this.http.patch<ContactChannelDto>(`${this.base}/admin/contact-channels/${key}`, { value }));
  }

  notifications() {
    return firstValueFrom(this.http.get<NotificationDto[]>(`${this.base}/notifications`));
  }

  markNotificationRead(id: number) {
    return firstValueFrom(this.http.patch(`${this.base}/notifications/${id}/read`, {}));
  }

  // ── Phase 1 ─────────────────────────────────────────────────────────────

  journeys() {
    return firstValueFrom(this.http.get<JourneyDto[]>(`${this.base}/journeys`));
  }
  journey(id: number) {
    return firstValueFrom(this.http.get<JourneyDetailDto>(`${this.base}/journeys/${id}`));
  }

  voc(filter?: { channel?: string; sentiment?: string }) {
    const params: any = {};
    if (filter?.channel) params.channel = filter.channel;
    if (filter?.sentiment) params.sentiment = filter.sentiment;
    return firstValueFrom(this.http.get<VocResponseDto[]>(`${this.base}/voc`, { params }));
  }
  updateVocComment(id: number, commentEn: string, commentAr: string) {
    return firstValueFrom(this.http.put<VocResponseDto>(`${this.base}/voc/${id}/comment`, { commentEn, commentAr }));
  }

  kbList(filter?: { category?: string; q?: string }) {
    const params: any = {};
    if (filter?.category) params.category = filter.category;
    if (filter?.q) params.q = filter.q;
    return firstValueFrom(this.http.get<KbArticleDto[]>(`${this.base}/kb`, { params }));
  }
  kbGet(id: number) {
    return firstValueFrom(this.http.get<KbArticleDto>(`${this.base}/kb/${id}`));
  }
  kbCreate(req: UpsertKbArticleRequest) {
    return firstValueFrom(this.http.post<KbArticleDto>(`${this.base}/kb`, req));
  }
  kbUpdate(id: number, req: UpsertKbArticleRequest) {
    return firstValueFrom(this.http.put<KbArticleDto>(`${this.base}/kb/${id}`, req));
  }
  kbDelete(id: number) {
    return firstValueFrom(this.http.delete(`${this.base}/kb/${id}`));
  }

  programme() {
    return firstValueFrom(this.http.get<ProgrammeInitiativeDto[]>(`${this.base}/programme`));
  }
  programmeStatus(id: number, req: UpdateProgrammeStatusRequest) {
    return firstValueFrom(this.http.put<ProgrammeInitiativeDto>(`${this.base}/programme/${id}/status`, req));
  }

  governanceBodies() {
    return firstValueFrom(this.http.get<GovernanceBodyDto[]>(`${this.base}/governance/bodies`));
  }
  governanceBody(id: number) {
    return firstValueFrom(this.http.get<GovernanceBodyDetailDto>(`${this.base}/governance/bodies/${id}`));
  }
  governanceDecision(bodyId: number, req: CreateGovernanceDecisionRequest) {
    return firstValueFrom(this.http.post<GovernanceDecisionDto>(`${this.base}/governance/bodies/${bodyId}/decisions`, req));
  }

  // ── Phase 2 ─────────────────────────────────────────────────────────────

  aboutList() {
    return firstValueFrom(this.http.get<AboutSectionDto[]>(`${this.base}/about`));
  }
  aboutUpdate(id: number, req: UpdateAboutSectionRequest) {
    return firstValueFrom(this.http.put<AboutSectionDto>(`${this.base}/about/${id}`, req));
  }

  architecture() {
    return firstValueFrom(this.http.get<ArchitectureReferenceDto>(`${this.base}/architecture`));
  }

  portalMyRequests() {
    return firstValueFrom(this.http.get<PortalRequestDto[]>(`${this.base}/portal/my-requests`));
  }
  portalCreate(req: CreatePortalRequestRequest) {
    return firstValueFrom(this.http.post<PortalRequestDto>(`${this.base}/portal`, req));
  }

  copilotAsk(req: AskCopilotRequest) {
    return firstValueFrom(this.http.post<CopilotInteractionDto>(`${this.base}/copilot/ask`, req));
  }
  copilotHistory() {
    return firstValueFrom(this.http.get<CopilotInteractionDto[]>(`${this.base}/copilot/history`));
  }

  auditEvents(filter: { userId?: number; kind?: string; from?: string; to?: string; page?: number; pageSize?: number } = {}) {
    const params: any = {};
    if (filter.userId !== undefined) params.userId = filter.userId;
    if (filter.kind) params.kind = filter.kind;
    if (filter.from) params.from = filter.from;
    if (filter.to) params.to = filter.to;
    if (filter.page) params.page = filter.page;
    if (filter.pageSize) params.pageSize = filter.pageSize;
    return firstValueFrom(this.http.get<AuditPageDto>(`${this.base}/audit/events`, { params }));
  }
  auditVerify() {
    return firstValueFrom(this.http.get<AuditVerifyResultDto>(`${this.base}/audit/verify`));
  }

  automationRules() {
    return firstValueFrom(this.http.get<AutomationRuleDto[]>(`${this.base}/automation/rules`));
  }
  automationToggle(id: number, enabled: boolean) {
    return firstValueFrom(this.http.put<AutomationRuleDto>(`${this.base}/automation/rules/${id}/enabled`, { enabled }));
  }
  automationRun(id: number) {
    return firstValueFrom(this.http.post<AutomationRunResultDto>(`${this.base}/automation/rules/${id}/run`, {}));
  }

  // ── Round 5 — Accessibility ─────────────────────────────────────────────
  a11yAudits() {
    return firstValueFrom(this.http.get<AccessibilityAuditDto[]>(`${this.base}/accessibility/audits`));
  }
  a11yAudit(id: number) {
    return firstValueFrom(this.http.get<AccessibilityAuditDetailDto>(`${this.base}/accessibility/audits/${id}`));
  }
  a11yUpdateRemediation(id: number, req: UpdateAccessibilityRemediationRequest) {
    return firstValueFrom(this.http.put<AccessibilityRemediationDto>(`${this.base}/accessibility/remediations/${id}`, req));
  }
  a11yWcagCriteria() {
    return firstValueFrom(this.http.get<WcagCriterionDto[]>(`${this.base}/accessibility/wcag-criteria`));
  }

  // ── Round 5 — Service health ────────────────────────────────────────────
  shMetrics(filter: { service?: string; from?: string; to?: string } = {}) {
    const params: any = {};
    if (filter.service) params.service = filter.service;
    if (filter.from) params.from = filter.from;
    if (filter.to) params.to = filter.to;
    return firstValueFrom(this.http.get<ServiceHealthMetricDto[]>(`${this.base}/service-health/metrics`, { params }));
  }
  shIncidents() {
    return firstValueFrom(this.http.get<ServiceIncidentDto[]>(`${this.base}/service-health/incidents`));
  }
  shChecks() {
    return firstValueFrom(this.http.get<SyntheticCheckDto[]>(`${this.base}/service-health/synthetic-checks`));
  }
  shToggleCheck(id: number, enabled: boolean) {
    return firstValueFrom(this.http.put<SyntheticCheckDto>(`${this.base}/service-health/synthetic-checks/${id}/enabled`, { enabled }));
  }
  shRunChecks() {
    return firstValueFrom(this.http.post<{ ran: number }>(`${this.base}/service-health/synthetic-checks/run`, {}));
  }

  // ── Round 5 — Continuous improvement ────────────────────────────────────
  impItems(filter: { stage?: string; source?: string; priority?: string } = {}) {
    const params: any = {};
    if (filter.stage) params.stage = filter.stage;
    if (filter.source) params.source = filter.source;
    if (filter.priority) params.priority = filter.priority;
    return firstValueFrom(this.http.get<ImprovementItemDto[]>(`${this.base}/improvement/items`, { params }));
  }
  impItem(id: number) {
    return firstValueFrom(this.http.get<ImprovementItemDetailDto>(`${this.base}/improvement/items/${id}`));
  }
  impCreate(req: CreateImprovementItemRequest) {
    return firstValueFrom(this.http.post<ImprovementItemDto>(`${this.base}/improvement/items`, req));
  }
  impTransition(id: number, req: TransitionPdcaRequest) {
    return firstValueFrom(this.http.post<ImprovementItemDetailDto>(`${this.base}/improvement/items/${id}/transition`, req));
  }
  impThresholds() {
    return firstValueFrom(this.http.get<KpiThresholdDto[]>(`${this.base}/improvement/kpi-thresholds`));
  }
  impUpdateThreshold(id: number, req: UpdateKpiThresholdRequest) {
    return firstValueFrom(this.http.put<KpiThresholdDto>(`${this.base}/improvement/kpi-thresholds/${id}`, req));
  }

  // ── Round 5 — CX analytics ──────────────────────────────────────────────
  cxaSnapshots(filter: { from?: string; to?: string; segment?: string } = {}) {
    const params: any = {};
    if (filter.from) params.from = filter.from;
    if (filter.to) params.to = filter.to;
    if (filter.segment) params.segment = filter.segment;
    return firstValueFrom(this.http.get<CxAnalyticsSnapshotDto[]>(`${this.base}/cx-analytics/snapshots`, { params }));
  }
  cxaTrend(segment: string, days: number) {
    return firstValueFrom(this.http.get<CxAnalyticsTrendDto>(`${this.base}/cx-analytics/trend`, { params: { segment, days } as any }));
  }
  cxaRootCauseLinks() {
    return firstValueFrom(this.http.get<RootCauseLinkDto[]>(`${this.base}/cx-analytics/root-cause-links`));
  }
  cxaCreateRootCauseLink(req: CreateRootCauseLinkRequest) {
    return firstValueFrom(this.http.post<RootCauseLinkDto>(`${this.base}/cx-analytics/root-cause-links`, req));
  }

  // ── Round 5 — Content governance ────────────────────────────────────────
  cgReviewCycles() {
    return firstValueFrom(this.http.get<ContentReviewCycleDto[]>(`${this.base}/content-governance/review-cycles`));
  }
  cgCreateCycle(req: CreateContentReviewCycleRequest) {
    return firstValueFrom(this.http.post<ContentReviewCycleDto>(`${this.base}/content-governance/review-cycles`, req));
  }
  cgUpdateCycle(id: number, req: UpdateContentReviewCycleRequest) {
    return firstValueFrom(this.http.put<ContentReviewCycleDto>(`${this.base}/content-governance/review-cycles/${id}`, req));
  }
  cgChannelPerformance(filter: { channel?: string; from?: string; to?: string } = {}) {
    const params: any = {};
    if (filter.channel) params.channel = filter.channel;
    if (filter.from) params.from = filter.from;
    if (filter.to) params.to = filter.to;
    return firstValueFrom(this.http.get<ChannelPerformanceMetricDto[]>(`${this.base}/content-governance/channel-performance`, { params }));
  }
  cgStaleArticles() {
    return firstValueFrom(this.http.get<StaleArticleDto[]>(`${this.base}/content-governance/stale-articles`));
  }
}
