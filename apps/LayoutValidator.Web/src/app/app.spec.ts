import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // As abas carregam catálogo de regras e layouts no init; sem isso o shell nem
      // renderiza, e o teste falharia por falta de provider em vez de por regressão.
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renderiza o título', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('LayoutValidator');
  });

  it('expõe as três abas', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const abas = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.mat-mdc-tab'),
    ).map((aba) => aba.textContent?.trim());

    expect(abas).toEqual(['Cadastrar', 'Listar', 'Testar']);
  });
});
