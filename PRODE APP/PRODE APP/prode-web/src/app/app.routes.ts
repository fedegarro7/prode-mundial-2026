import { Routes } from '@angular/router';

import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { MatchesComponent } from './pages/matches/matches.component';
import { AdminComponent } from './pages/admin/admin.component';
import { StandingsComponent } from './pages/standings/standings.component';
import { GroupsComponent } from './pages/groups/groups.component';
import { RankingsComponent } from './pages/rankings/rankings.component';
import { AccountComponent } from './pages/account/account.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [

	{
		path: '',
		component: HomeComponent
	},

	{
		path: 'login',
		component: LoginComponent
	},

	{
		path: 'register',
		component: RegisterComponent
	},

	{
    path: 'matches',
    component: MatchesComponent,
    canActivate: [authGuard]
  },

	{
    path: 'admin',
    component: AdminComponent,
    canActivate: [authGuard]
  },

	{
    path: 'standings',
    component: StandingsComponent
  },

	{
    path: 'groups',
    component: GroupsComponent,
    canActivate: [authGuard]
  },

	{
    path: 'rankings',
    component: RankingsComponent,
    canActivate: [authGuard]
  },

	{
    path: 'account',
    component: AccountComponent,
    canActivate: [authGuard]
  },

];

