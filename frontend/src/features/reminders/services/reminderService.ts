import { api } from '../../../core/api/client';

export interface ReminderDateItem {
  id: string;
  title: string;
  recipientName: string;
  occasionDate: string;
  remindDaysBefore: number;
  notes?: string;
}

export interface CreateReminderDateRequest {
  title: string;
  recipientName: string;
  occasionDate: string;
  remindDaysBefore: number;
  notes?: string;
}

export const reminderService = {
  async getReminders(): Promise<ReminderDateItem[]> {
    const res = await api.get<ReminderDateItem[]>('/ReminderDates');
    return res.data;
  },

  async createReminder(data: CreateReminderDateRequest): Promise<any> {
    const res = await api.post('/ReminderDates', data);
    return res.data;
  },

  async deleteReminder(id: string): Promise<any> {
    const res = await api.delete(`/ReminderDates/${id}`);
    return res.data;
  },
};

