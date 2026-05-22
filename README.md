# Penumbra Drifter

> **그림자를 숨는 장소가 아니라, 이동하고 행동할 수 있는 공간으로 재해석한 2.5D 잠입 액션·퍼즐 게임**

**Penumbra Drifter**는 플레이어가 빛을 피하고 그림자를 이용해 이동하며, 오브젝트와 광원을 활용해 새로운 그림자 경로를 만들어 목표 지점까지 잠입하는 Unity 게임 프로젝트입니다.

본 프로젝트는 현재 제작 중이며, 포트폴리오 확인 시에는 **`develop` 브랜치**에서 현재까지 구현된 결과물을 확인할 수 있습니다.

- Repository: https://github.com/kbwon/PenumbraDrifter.git
- Current Development Branch: `develop`
- Status: **In Development / 제작 중**
- Engine: **Unity 6**
- Language: **C#**
- Target Platform: **PC / Windows**

---

## Overview

일반적인 잠입 게임에서 그림자는 주로 적에게 들키지 않기 위한 은신 공간으로 사용됩니다.  
하지만 **Penumbra Drifter**에서는 그림자 자체가 플레이어의 이동 가능 영역이자 핵심 플레이 공간입니다.

플레이어는 단순히 길을 찾는 것이 아니라,  
빛과 오브젝트의 관계를 파악하고 그림자를 연결하여 직접 침투 루트를 설계합니다.

### Core Concept

- **빛이 닿는 곳은 위험 구역**
  - 적에게 발견되거나 그림자 능력 사용이 제한됩니다.

- **그림자는 이동 가능한 공간**
  - 플레이어는 그림자 위에서 특수 이동과 잠입 행동을 수행할 수 있습니다.

- **환경을 조작해 길을 만든다**
  - 오브젝트, 구조물, 광원에 의해 생기는 그림자를 이용해 새로운 경로를 만듭니다.

- **잠입과 퍼즐의 결합**
  - 적의 시야와 순찰을 피해 그림자 루트를 설계하고, 필요한 경우 그림자 속에서 암살 행동을 수행합니다.

---

## Current Development Status

이 프로젝트는 아직 완성된 정식 출시 버전이 아니며, 현재도 기능 구현과 스테이지 제작, 밸런싱, 연출 개선이 진행 중입니다.

현재까지의 주요 결과물은 `develop` 브랜치에 반영되어 있습니다.

```bash
git clone -b develop --single-branch https://github.com/kbwon/PenumbraDrifter.git
