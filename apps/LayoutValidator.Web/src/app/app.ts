import { Component, ViewChild, signal } from '@angular/core';
import { MatTabGroup, MatTabsModule } from '@angular/material/tabs';
import { LayoutForm } from './components/layout-form/layout-form';
import { LayoutList } from './components/layout-list/layout-list';
import { LayoutTest } from './components/layout-test/layout-test';
import { Layout } from './models/layout.model';

@Component({
  selector: 'app-root',
  imports: [MatTabsModule, LayoutForm, LayoutList, LayoutTest],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  @ViewChild('tabs') tabs?: MatTabGroup;
  @ViewChild(LayoutList) layoutList?: LayoutList;
  @ViewChild(LayoutTest) layoutTest?: LayoutTest;

  protected readonly title = signal('LayoutValidator');
  protected readonly layoutEmEdicao = signal<Layout | null>(null);

  editarLayout(layout: Layout): void {
    this.layoutEmEdicao.set(layout);
    if (this.tabs) {
      this.tabs.selectedIndex = 0;
    }
  }

  aoSalvarLayout(): void {
    this.layoutEmEdicao.set(null);
    this.layoutList?.recarregar();
    if (this.tabs) {
      this.tabs.selectedIndex = 1;
    }
  }

  aoTrocarAba(indice: number): void {
    if (indice === 1) {
      this.layoutList?.recarregar();
    } else if (indice === 2) {
      this.layoutTest?.recarregar();
    }
  }
}
