export type TicketCategory = {
  id?: number;
  name: string;
  basePrice: number;
  totalQuantity: number;
  soldQuantity: number;
  effectivePrice?: number;
  xmin?: number;
};

export type EventDto = {
  id: number;
  title: string;
  venue: string;
  description: string;
  eventDate: string;
  pricingStrategy: 0 | 1;
  xmin: number;
  categories: TicketCategory[];
};

export type EventListItem = {
  id: number;
  title: string;
  venue: string;
  eventDate: string;
  minPrice: number | null;
  availableTickets: number;
};

export type OrderResponse = {
  id: number;
  status: 0 | 1 | 2;
  totalAmount: number;
  createdAt: string;
  items: {
    ticketCategoryId: number;
    ticketCategoryName: string;
    eventTitle: string;
    quantity: number;
    unitPrice: number;
  }[];
};
