function pad(value: number): string {
  return value.toString().padStart(2, '0');
}

/** "2030-01-15", no fuso do navegador (formato do <input type="date">). */
export function toDateInputValue(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** "14:30", no fuso do navegador (formato do <input type="time">). */
export function toTimeInputValue(date: Date): string {
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Sugestão inicial para agendamento: daqui a ~15 minutos, arredondado para múltiplo de 5. */
export function suggestedScheduleDate(now: Date): Date {
  const suggestion = new Date(now.getTime() + 15 * 60 * 1000);
  suggestion.setMinutes(Math.ceil(suggestion.getMinutes() / 5) * 5, 0, 0);
  return suggestion;
}
