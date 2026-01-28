import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import katex from 'katex';

/**
 * LatexRendererComponent - Renders text containing LaTeX math expressions
 * 
 * Supports:
 * - Inline math: $...$ or \(...\)
 * - Block/Display math: $$...$$ or \[...\]
 * 
 * Usage:
 *   <app-latex-renderer [content]="questionText"></app-latex-renderer>
 *   <app-latex-renderer [content]="noteContent" [displayMode]="true"></app-latex-renderer>
 */
@Component({
  selector: 'app-latex-renderer',
  standalone: true,
  imports: [CommonModule],
  template: `<span #container [innerHTML]="renderedHtml"></span>`,
  styles: [`
    :host {
      display: inline;
    }
    :host ::ng-deep .katex {
      font-size: 1.1em;
    }
    :host ::ng-deep .katex-display {
      margin: 0.5em 0;
      text-align: center;
    }
    :host ::ng-deep .latex-error {
      color: #cc0000;
      font-family: monospace;
      font-size: 0.9em;
    }
  `]
})
export class LatexRendererComponent implements OnChanges, AfterViewInit {
  @Input() content: string = '';
  @Input() displayMode: boolean = false;

  @ViewChild('container') container!: ElementRef;

  renderedHtml: string = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['content']) {
      this.render();
    }
  }

  ngAfterViewInit(): void {
    this.render();
  }

  private render(): void {
    if (!this.content) {
      this.renderedHtml = '';
      return;
    }

    try {
      this.renderedHtml = this.parseLatex(this.content);
    } catch (error) {
      console.error('LaTeX rendering error:', error);
      this.renderedHtml = this.escapeHtml(this.content);
    }
  }

  /**
   * Parse text and replace LaTeX delimiters with rendered KaTeX HTML
   */
  private parseLatex(text: string): string {
    let result = text;

    // Process block math first ($$...$$ and \[...\])
    // $$...$$
    result = result.replace(/\$\$([\s\S]*?)\$\$/g, (match, latex) => {
      return this.renderKatex(latex.trim(), true);
    });

    // \[...\]
    result = result.replace(/\\\[([\s\S]*?)\\\]/g, (match, latex) => {
      return this.renderKatex(latex.trim(), true);
    });

    // Process inline math ($...$ and \(...\))
    // \(...\)
    result = result.replace(/\\\(([\s\S]*?)\\\)/g, (match, latex) => {
      return this.renderKatex(latex.trim(), false);
    });

    // $...$ (but not $$)
    // Use negative lookbehind/lookahead to avoid matching $$ 
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
          // Common macros
          "\\R": "\\mathbb{R}",
          "\\N": "\\mathbb{N}",
          "\\Z": "\\mathbb{Z}",
          "\\Q": "\\mathbb{Q}",
          "\\C": "\\mathbb{C}"
        }
      });
    } catch (error) {
      console.warn('KaTeX render error for:', latex, error);
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
