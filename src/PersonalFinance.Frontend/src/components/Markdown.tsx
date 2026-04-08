import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { cn } from '../lib/utils';

interface MarkdownProps {
  content: string;
  className?: string;
}

export default function Markdown({ content, className }: MarkdownProps) {
  return (
    <div
      className={cn(
        'prose prose-sm max-w-none',
        'prose-p:text-[var(--color-text)] prose-p:leading-relaxed prose-p:my-2 prose-p:first:mt-0 prose-p:last:mb-0',
        'prose-pre:bg-[var(--color-surface)] prose-pre:text-[var(--color-text)] prose-pre:border prose-pre:border-[var(--color-border)] prose-pre:rounded-lg',
        'prose-code:bg-[var(--color-surface)] prose-code:text-[var(--color-text)] prose-code:px-1 prose-code:py-0.5 prose-code:rounded prose-code:before:content-none prose-code:after:content-none',
        'prose-strong:text-[var(--color-text)] prose-strong:font-semibold',
        'prose-a:text-[var(--color-primary-light)] prose-a:no-underline hover:prose-a:underline',
        'prose-blockquote:border-l-[var(--color-primary)] prose-blockquote:text-[var(--color-text-muted)] prose-blockquote:italic',
        'prose-h1:text-[var(--color-text)] prose-h1:font-bold prose-h1:text-xl prose-h1:mt-3 prose-h1:mb-2',
        'prose-h2:text-[var(--color-text)] prose-h2:font-bold prose-h2:text-lg prose-h2:mt-3 prose-h2:mb-1',
        'prose-h3:text-[var(--color-text)] prose-h3:font-semibold prose-h3:text-base prose-h3:mt-2 prose-h3:mb-1',
        'prose-ul:my-2 prose-ul:list-disc prose-ul:pl-6',
        'prose-ol:my-2 prose-ol:list-decimal prose-ol:pl-6',
        'prose-li:my-1',
        'prose-table:border-collapse prose-table:w-full prose-table:my-3',
        'prose-th:border prose-th:border-[var(--color-border)] prose-th:bg-[var(--color-surface-elevated)] prose-th:px-3 prose-th:py-2 prose-th:text-left prose-th:font-semibold prose-th:text-[var(--color-text)]',
        'prose-td:border prose-td:border-[var(--color-border)] prose-td:px-3 prose-td:py-2 prose-td:text-[var(--color-text-muted)]',
        'prose-hr:border-[var(--color-border)] prose-hr:my-3',
        className,
      )}
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          a: ({ node, ...props }) => (
            <a {...props} target="_blank" rel="noopener noreferrer" />
          ),
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
