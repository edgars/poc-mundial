import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'entrar' },
  { path: 'entrar', loadComponent: () => import('./entrar/entrar').then(m => m.Entrar) },
  { path: 'docas', loadComponent: () => import('./docas/docas').then(m => m.Docas) },
  { path: 'conferencia/:documento', loadComponent: () => import('./conferencia/conferencia').then(m => m.Conferencia) },
  { path: 'codigos', loadComponent: () => import('./codigos/codigos').then(m => m.Codigos) },
  { path: 'consultas', loadComponent: () => import('./consultas/consultas').then(m => m.Consultas) },
  { path: '**', redirectTo: 'entrar' },
];
