import * as React from "react";

type ModalProps = {
    open: boolean;
    children: React.ReactNode;
    /** Tailwind width/height classes for the panel */
    className?: string;
    z?: "z-20" | "z-30" | "z-40";
};

// Shared overlay shell: scrim + one surface panel. Fades with opacity only.
export default function Modal({ open, children, className = "", z = "z-30" }: ModalProps) {
    return (
        <div
            aria-hidden={!open}
            className={`fixed inset-0 ${z} flex items-center justify-center bg-scrim transition-opacity duration-200 ${open ? "opacity-100" : "opacity-0 pointer-events-none"}`}
        >
            <div
                role="dialog"
                aria-modal="true"
                className={`flex flex-col bg-surface text-ink border border-line rounded-panel shadow-panel overflow-hidden ${className}`}
            >
                {children}
            </div>
        </div>
    );
}

type ModalHeaderProps = {
    title: React.ReactNode;
    onClose?: () => void;
    closeLabel?: string;
    children?: React.ReactNode;
};

export function ModalHeader({ title, onClose, closeLabel, children }: ModalHeaderProps) {
    return (
        <div className="flex items-center gap-3 px-5 h-14 border-b border-line shrink-0">
            <h2 className="text-md font-semibold flex-1 truncate">{title}</h2>
            {children}
            {onClose && (
                <button
                    type="button"
                    aria-label={closeLabel ?? "Close"}
                    onClick={onClose}
                    className="w-9 h-9 grid place-items-center rounded-control text-muted hover:text-ink hover:bg-raised transition-colors"
                >
                    <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true">
                        <path d="M3.5 3.5l9 9m0-9l-9 9" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
                    </svg>
                </button>
            )}
        </div>
    );
}
