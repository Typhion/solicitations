// Full class strings (not built dynamically) so Tailwind picks them up when scanning.
// Tinted fill + solid left edge, using the --cal-* tokens that flip with the theme.
const eventClasses = [
    'bg-cal-1/15 border-cal-1 hover:bg-cal-1/25',
    'bg-cal-2/15 border-cal-2 hover:bg-cal-2/25',
    'bg-cal-3/15 border-cal-3 hover:bg-cal-3/25',
    'bg-cal-4/15 border-cal-4 hover:bg-cal-4/25',
    'bg-cal-5/15 border-cal-5 hover:bg-cal-5/25',
    'bg-cal-6/15 border-cal-6 hover:bg-cal-6/25',
    'bg-cal-7/15 border-cal-7 hover:bg-cal-7/25',
    'bg-cal-8/15 border-cal-8 hover:bg-cal-8/25',
];

const dotClasses = [
    'bg-cal-1',
    'bg-cal-2',
    'bg-cal-3',
    'bg-cal-4',
    'bg-cal-5',
    'bg-cal-6',
    'bg-cal-7',
    'bg-cal-8',
];

/** Stable palette slot for a solicitation id, so its meetings always share a colour. */
function slot(solicitationId: string): number {
    let hash = 0;
    for (const char of solicitationId) hash = (hash * 31 + char.charCodeAt(0)) | 0;
    return Math.abs(hash) % eventClasses.length;
}

export function eventColor(solicitationId: string): string {
    return eventClasses[slot(solicitationId)];
}

export function dotColor(solicitationId: string): string {
    return dotClasses[slot(solicitationId)];
}
