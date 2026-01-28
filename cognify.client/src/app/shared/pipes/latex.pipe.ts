import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import katex from 'katex';

/**
 * LatexPipe - Transform text containing LaTeX into rendered HTML
 * 
 * Usage:
 *   {{ questionText | latex }}
 *   <span [innerHTML]="noteContent | latex"></span>
 */
@Pipe({
    name: 'latex',
    standalone: true
})
export class LatexPipe implements PipeTransform {
    constructor(private sanitizer: DomSanitizer) { }

    transform(value: string | null | undefined): SafeHtml {
        if (!value) {
            return '';
        }

        try {
            const rendered = this.parseLatex(value);
            return this.sanitizer.bypassSecurityTrustHtml(rendered);
        } catch (error) {
            console.error('LaTeX pipe error:', error);
            return value;
        }
    }

    /**
     * Parse text and replace LaTeX delimiters with rendered KaTeX HTML
     */
    private parseLatex(text: string): string {
        let result = text;

        // Process block math first ($$...$$ and \[...\])
        result = result.replace(/\$\$([\s\S]*?)\$\$/g, (match, latex) => {
            return this.renderKatex(latex.trim(), true);
        });

        result = result.replace(/\\\[([\s\S]*?)\\\]/g, (match, latex) => {
            return this.renderKatex(latex.trim(), true);
        });

        // Process inline math ($...$ and \(...\))
        result = result.replace(/\\\(([\s\S]*?)\\\)/g, (match, latex) => {
            return this.renderKatex(latex.trim(), false);
        });

        // $...$ (but not $$)
        result = result.replace(/(?<!\$)\$(?!\$)(.*?)(?<!\$)\$(?!\$)/g, (match, latex) => {
            return this.renderKatex(latex.trim(), false);
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
            return `<span style="color: #cc0000; font-family: monospace;">[LaTeX Error]</span>`;
        }
    }
}
