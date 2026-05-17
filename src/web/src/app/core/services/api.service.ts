import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  ComplaintDto, ComplaintListItemDto, ComplaintsByCategoryDto, ContactChannelDto,
  InboxThreadDto, KpiDto, NotificationDto, RolePermissionDto,
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
}
