> **This fork is an experiment, not a product.** It is a public proof-of-concept for
> **ASOP-driven legacy modernization**: taking one vertical slice of this .NET Framework
> codebase to .NET 10 at a time, using a written, versioned procedure (an
> [ASOP](https://github.com/mabidoli/asop) — Agentic Standard Operating Procedure) where
> every step is gated by CI evidence (build/test/golden-file diff), not by an agent's own
> say-so. It is unaffiliated with the original project or its maintainer.
>
> - Upstream (the real thing): [shanselman/dasblog](https://github.com/shanselman/dasblog)
> - A human-written modern port, used here only as an independent reference point (not a
>   dependency of this POC): [poppastring/dasblog-core](https://github.com/poppastring/dasblog-core)
> - The ASOP that drives the modernization work, and the evidence for each run, live under
>   [`asop/`](./asop) in this repo.
> - **Clone with `git clone --recursive`** — [`harness/`](./harness) is a git submodule
>   ([agentic-co/agentic-co-harness](https://github.com/agentic-co/agentic-co-harness)); a
>   plain clone leaves it empty (`git submodule update --init --recursive` fixes an existing
>   checkout). Running on Windows: see [`RUNBOOK-WINDOWS.md`](./RUNBOOK-WINDOWS.md). Plan and
>   iteration loop: [`PLAN.md`](./PLAN.md).
>
> Everything below the next heading is the original upstream README, unmodified.

# dasblog
The old, wonderful, and scalable DasBlog Blogging Engine

# Appveyor Build Server Status
[![Build status](https://ci.appveyor.com/api/projects/status/kdhfio56prwd0m0f?svg=true)](https://ci.appveyor.com/project/ScottHanselman/dasblog)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fshanselman%2Fdasblog.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fshanselman%2Fdasblog?ref=badge_shield)


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fshanselman%2Fdasblog.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fshanselman%2Fdasblog?ref=badge_large)