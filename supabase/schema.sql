-- Diario de Gym: esquema para Supabase (PostgreSQL).
-- Ejecútalo una sola vez en Supabase > SQL Editor > New query > Run.
-- Cada tabla tiene Row Level Security: cada usuario solo puede ver y modificar sus propias filas.

-- Perfil y ajustes de cada usuario (una fila por usuario)
create table if not exists public.perfiles (
  user_id      uuid primary key default auth.uid() references auth.users (id) on delete cascade,
  meta_semanal smallint not null default 4 check (meta_semanal between 1 and 7),
  peso_meta    numeric(5,1) check (peso_meta > 20 and peso_meta < 400),
  altura       numeric(4,1) check (altura between 100 and 250),
  sexo         char(1) check (sexo in ('h','m')),
  nacimiento   date,
  actualizado  timestamptz not null default now()
);

-- Rutinas: días de la semana (0 = lunes ... 6 = domingo) y ejercicios [{nombre, series, reps, peso}]
create table if not exists public.rutinas (
  id         uuid primary key default gen_random_uuid(),
  user_id    uuid not null default auth.uid() references auth.users (id) on delete cascade,
  nombre     text not null check (length(nombre) between 1 and 80),
  dias       smallint[] not null default '{}',
  ejercicios jsonb not null default '[]',
  orden      integer not null default 0
);
create index if not exists rutinas_user_idx on public.rutinas (user_id);

-- Días que asistió al gym (uno por fecha)
create table if not exists public.asistencias (
  user_id   uuid not null default auth.uid() references auth.users (id) on delete cascade,
  fecha     date not null,
  rutina_id uuid references public.rutinas (id) on delete set null,
  ts        bigint,
  primary key (user_id, fecha)
);

-- Cada serie registrada (peso × repeticiones)
create table if not exists public.registros (
  id        uuid primary key default gen_random_uuid(),
  user_id   uuid not null default auth.uid() references auth.users (id) on delete cascade,
  fecha     date not null,
  ejercicio text not null check (length(ejercicio) between 1 and 80),
  peso      numeric(6,2) not null check (peso >= 0),
  reps      integer not null check (reps > 0),
  ts        bigint,
  rutina_id uuid references public.rutinas (id) on delete set null
);
create index if not exists registros_user_fecha_idx on public.registros (user_id, fecha);

-- Peso corporal (uno por fecha)
create table if not exists public.pesos (
  user_id uuid not null default auth.uid() references auth.users (id) on delete cascade,
  fecha   date not null,
  kg      numeric(5,1) not null check (kg > 20 and kg < 400),
  primary key (user_id, fecha)
);

-- Seguridad por fila: el dueño de la fila es el único que la ve y la cambia
do $$
declare t text;
begin
  foreach t in array array['perfiles','rutinas','asistencias','registros','pesos'] loop
    execute format('alter table public.%I enable row level security', t);
    execute format('drop policy if exists "solo el dueño" on public.%I', t);
    execute format(
      'create policy "solo el dueño" on public.%I for all to authenticated
         using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id)', t);
  end loop;
end $$;
