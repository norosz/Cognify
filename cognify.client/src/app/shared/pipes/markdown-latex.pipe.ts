import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import katex from 'katex';

/**
 * MarkdownLatexPipe - Transform Markdown text with embedded LaTeX into rendered HTML
 * 
 * Supports:
 * - All standard Markdown syntax (headers, bold, italic, lists, code, etc.)
 * - Inline math: $...$ or \(...\)
 * - Block/Display math: $$...$$ or \[...\]
 * 
 * Usage:
 *   <div [innerHTML]="noteContent | markdownLatex"></div>
 */
@Pipe({
    name: 'markdownLatex',
    standalone: true
})
export class MarkdownLatexPipe implements PipeTransform {
    constructor(private sanitizer: DomSanitizer) {
        // Configure marked options
        marked.setOptions({
            breaks: true,      // Convert \n to <br>
            gfm: true,         // GitHub Flavored Markdown
        });
    }

    transform(value: string | null | undefined): SafeHtml {
        if (!value) {
            return '';
        }

        try {
            // First, protect LaTeX from Markdown processing by replacing with placeholders
            const { text, placeholders } = this.protectLatex(value);

            // Parse Markdown
            const markdownHtml = marked.parse(text) as string;

            // Restore and render LaTeX
            const finalHtml = this.restoreAndRenderLatex(markdownHtml, placeholders);

            return this.sanitizer.bypassSecurityTrustHtml(finalHtml);
        } catch (error) {
            console.error('Markdown/LaTeX pipe error:', error);
            return value;
        }
    }

    /**
     * Protect LaTeX expressions from Markdown processing
     */
    private protectLatex(text: string): { text: string; placeholders: Map<string, { latex: string; display: boolean }> } {
        const placeholders = new Map<string, { latex: string; display: boolean }>();
        let counter = 0;

        const createPlaceholder = (latex: string, display: boolean): string => {
            // usage of special characters like underscores can be messed up by markdown
            // so we use a very simple alphanumeric string
            const id = `MATH${counter++}LATEX`;
            placeholders.set(id, { latex, display });
            return id;
        };

        let result = text;

        // Protect block math first
        // $$...$$
        result = result.replace(/\$\$([\s\S]*?)\$\$/g, (match, latex) => {
            return createPlaceholder(latex.trim(), true);
        });

        // \[...\]
        result = result.replace(/\\\[([\s\S]*?)\\\]/g, (match, latex) => {
            return createPlaceholder(latex.trim(), true);
        });

        // Protect inline math
        // \(...\)
        result = result.replace(/\\\(([\s\S]*?)\\\)/g, (match, latex) => {
            return createPlaceholder(latex.trim(), false);
        });

        // $...$ (but not $$)
        result = result.replace(/(?<!\$)\$(?!\$)([^\$\n]+?)(?<!\$)\$(?!\$)/g, (match, latex) => {
            return createPlaceholder(latex.trim(), false);
        });

        return { text: result, placeholders };
    }

    /**
     * Restore placeholders with rendered KaTeX
     */
    private restoreAndRenderLatex(html: string, placeholders: Map<string, { latex: string; display: boolean }>): string {
        let result = html;

        placeholders.forEach((value, key) => {
            const rendered = this.renderKatex(value.latex, value.display);
            result = result.replace(key, rendered);
        });

        return result;
    }

    /**
     * Render LaTeX string to HTML using KaTeX
     */
    private renderKatex(latex: string, displayMode: boolean): string {
        try {
            return katex.renderToString(latex, {
                displayMode: displayMode,
                throwOnError: false,
                errorColor: '#cc0000',
                strict: false,
                trust: true,
                macros: {
                    "\\R": "\\mathbb{R}",
                    "\\N": "\\mathbb{N}",
                    "\\Z": "\\mathbb{Z}",
                    "\\Q": "\\mathbb{Q}",
                    "\\C": "\\mathbb{C}"
                }
            });
        } catch (error) {
            console.warn('KaTeX render error:', latex, error);
            return `<span class="latex-error">[LaTeX Error: ${this.escapeHtml(latex)}]</span>`;
        }
    }

    /**
     * Escape HTML special characters
     */
    private escapeHtml(text: string): string {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}
