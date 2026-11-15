# Third-party notices

## Scope of the project's MIT License

The root [LICENSE](LICENSE) applies to original contributions owned by the project's copyright holders. Third-party packages, SDKs, imported Unity resources, fonts, and externally sourced media retain their respective terms. Inclusion in this repository does not grant additional redistribution rights.

The root license identifies the team's original code as Copyright (c) 2026 Clarity Labs. Contributors must have authority to license their contributions. Third-party material retains its respective licenses; the repository is not represented as consisting entirely of MIT-licensed material.

This inventory was prepared from the repository at `31083f7`, its package manifest and lockfile, installed package license/notice files, the installed Unity template archive, and the installed Gemma model license. It is a source/package audit, not a complete inventory of a shipped APK. A dependency listing does not establish that every component is included in a player build.

## Project-developed code and imported content

The procedure integration under `XRProcedualCopilot/Assets/Scripts/Procedure/`, the project-specific controller smoke test, the Python companion and generators, the companion web interface, and project tests appear to be project-developed contributions based on their purpose and Git history. This classification is not proof of legal ownership.

Imported Unity template scripts and icons, TextMesh Pro resources, fonts, package contents, copied transcripts, and externally sourced tutorial data must not be relabeled as original team-authored MIT code.

## TLab WebView and virtual keyboard

| Package | Resolved version | Pinned source | Verified terms |
|---|---|---|---|
| `com.tlabaltoh.webview` | 1.0.8 | [TLabWebView, commit 3bd84ecb1796312ae871ed94f2501a63e0b25492](https://github.com/TLabAltoh/TLabWebView/tree/3bd84ecb1796312ae871ed94f2501a63e0b25492) | MIT |
| `com.tlabaltoh.vkeyborad` | 1.0.2 | [TLabVKeyborad, commit 2b2755158be4a28d13384f22415782d57b9efab4](https://github.com/TLabAltoh/TLabVKeyborad/tree/2b2755158be4a28d13384f22415782d57b9efab4) | MIT |

Both installed `LICENSE.md` files contain the following copyright and MIT notice. Preserve this notice when redistributing copies or substantial portions of these components.

```text
MIT License

Copyright (c) 2023 tlabaltoh

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

The WebView package contains `libTLabWebView-release.aar`. No license/notice entries were found inside that archive. The package-level MIT license was verified, but a complete dependency inventory of the embedded binary was not established by this audit.

## Meta Voice SDK and Wit.ai

`com.meta.xr.sdk.voice` version **85.0.1** is governed by the [Meta Platform Technologies SDK License Agreement](https://developers.meta.com/horizon/licenses/oculussdk/), not MIT. Its installed `LICENSE.md` states:

```text
Copyright © Meta Platform Technologies, LLC and its affiliates.
All rights reserved.

Licensed under the Meta Platform Technologies SDK License Agreement (the "License");
you may not use the MPT SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developers.meta.com/horizon/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the MPT SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

The SDK's `Lib/Wit.ai/LICENSE` has separate, service-specific terms. This is not the standard MIT License:

```text
Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

You are hereby granted a non-exclusive, worldwide, royalty-free license to
use, copy, modify, and distribute this software in source code or binary
form for use in connection with the web services and APIs provided by
Wit.ai.

As with any software that integrates with the Wit.ai platform, your use
of this software is subject to the Wit.ai Terms of Service
[https://wit.ai/terms]. This copyright notice shall be included in all
copies or substantial portions of the software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
```

Additional license files inside Voice SDK identify:

- **NativeWebSocket:** Apache License 2.0; copyright 2019 Endel Dreyer and 2018 Jiri Hybek.
- **UnityOpus:** MIT; copyright 2018 TyounanMOTI.
- **NLayer:** MIT; copyright 2018 Mark Heath, Andrew Ward & Contributors.

Preserve their supplied notices and applicable full licenses when redistributing those components. Meta SDK/service terms remain separate from this project's MIT grant.

## Unity packages

Versions below come from the audited resolved lockfile and installed metadata. For example, the manifest requests OpenXR 1.16.1 while the lockfile resolves 1.18.0. License classifications come from installed `LICENSE.md` files, not an assumption that Unity packages share one license.

- **UCL:** [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license).
- **UPDL:** [Unity Package Distribution License](https://unity.com/legal/licenses/unity-package-distribution-license).
- Other named licenses remain distinct, including the Searcher package's supplied Unity Companion Package License v1.0.

| Package | Resolved version | Verified license category |
|---|---|---|
| `com.unity.ai.navigation` | 2.0.16 | UCL |
| `com.unity.burst` | 1.8.31 | UCL for source; UPDL otherwise |
| `com.unity.collab-proxy` | 2.14.0 | UPDL |
| `com.unity.collections` | 2.7.1 | UCL |
| `com.unity.editorcoroutines` | 1.1.0 | UCL |
| `com.unity.ext.nunit` | 2.0.5 | UPDL |
| `com.unity.ide.rider` | 3.1.0 | MIT |
| `com.unity.ide.visualstudio` | 2.0.26 | MIT |
| `com.unity.inputsystem` | 1.20.1 | UCL |
| `com.unity.mathematics` | 1.3.3 | UCL |
| `com.unity.multiplayer.center` | 1.0.1 | UPDL |
| `com.unity.nuget.mono-cecil` | 1.11.6 | UCL |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 | UCL |
| `com.unity.pipeline` | 1.0.0 | Unity Terms of Service |
| `com.unity.render-pipelines.core` | 17.3.0 | UCL |
| `com.unity.render-pipelines.universal-config` | 17.0.3 | UCL |
| `com.unity.render-pipelines.universal` | 17.3.0 | UCL |
| `com.unity.searcher` | 4.9.5 | Unity Companion Package License v1.0 |
| `com.unity.shadergraph` | 17.3.0 | UCL |
| `com.unity.test-framework.performance` | 3.5.0 | UCL |
| `com.unity.test-framework` | 1.6.0 | UCL |
| `com.unity.timeline` | 1.8.13 | UCL |
| `com.unity.ugui` | 2.0.0 | UCL |
| `com.unity.visualscripting` | 1.9.12 | UPDL |
| `com.unity.xr.arfoundation` | 6.6.0 | UCL |
| `com.unity.xr.compositionlayers` | 2.6.1 | UCL for source; UPDL otherwise |
| `com.unity.xr.core-utils` | 2.6.0 | UCL |
| `com.unity.xr.legacyinputhelpers` | 2.1.13 | UCL |
| `com.unity.xr.management` | 4.7.0 | UCL |
| `com.unity.xr.meta-openxr` | 2.7.0 | UCL for source; UPDL otherwise |
| `com.unity.xr.openxr` | 1.18.0 | UCL for source; UPDL otherwise |

Preserve Unity copyright/license notices and the packages' separately supplied third-party notices where their components are redistributed. The MIT-licensed IDE integrations have their own copyright holders: the Rider integration includes Unity Technologies and JetBrains notices; the Visual Studio integration includes Unity Technologies and Microsoft notices.

Built-in `com.unity.modules.*` dependencies are engine components. No separate license text was located in their package directories; this inventory does not classify them as MIT. Unity engine use and distribution remain subject to the applicable Unity terms.

### Nested Unity third-party notices

Installed packages contain additional notice files, including the following verified entries. Package wrappers and their nested components can have different licenses. Keep the relevant complete upstream notice files with redistributed components; this summary is not a replacement for their license texts.

| Package or area | Verified nested components and terms |
|---|---|
| Burst | LLVM: Apache 2.0 with LLVM exceptions and legacy University of Illinois/NCSA notices; Mono.Cecil: MIT; Smash and xxHash: BSD 2-Clause; musl: MIT; SLEEF: Boost Software License 1.0; gRPC for .NET: Apache 2.0; Google.Protobuf: BSD-style notice; mimalloc: MIT |
| Core rendering | RadeonRays 4.1: MIT; Bullet Physics SDK: zlib; Sobol sampler: MIT |
| Universal rendering | NVIDIA FXAA: the supplied FXAA3_11.h license |
| OpenXR | Khronos OpenXR-SDK-Source: Apache 2.0; Oculus OpenXR Mobile SDK: Oculus SDK License Agreement |
| Meta OpenXR | Khronos OpenXR-SDK-Source: Apache 2.0; Oculus OpenXR Mobile SDK and AudioPluginOculusSpatializer: Oculus SDK License Agreement |
| Legacy Input Helpers | Google arm-model code: Apache 2.0 |
| Version Control/Collab Proxy | File System Watcher and Json.NET: MIT; Zlib64: zlib; LZ4 and Antlr: BSD; Log4Net: Apache 2.0 |
| NUnit wrapper | NUnit: MIT |
| Mono.Cecil wrapper | Mono.Cecil: MIT |
| Newtonsoft.Json wrapper | Newtonsoft.Json, Json.Net.Unity3D, Newtonsoft.Json-for-Unity, and com.newtonsoft.json: MIT |
| Performance Testing | Perfolizer: MIT |
| Pipeline | Modified Roslyn assemblies and listed System.* dependencies: MIT, with their supplied modification and attribution notices |
| Visual Studio integration | VSWhere: MIT; benbuck/EnvDTE: Zero-Clause BSD |
| Visual Scripting | AQN Parser and DotNetZip: Microsoft Public License; FatCow Icons: CC BY 3.0; Iconmonstr Icons: bespoke terms; SQLite: public-domain declaration; Antlr 3 Runtime: BSD; Deep Copy, Full Serializer, MD4 Managed Implementation, Reorderable List, SQLite .NET, YamlDotNet, Ensure.That, and NCalc: MIT |

## Imported Unity template and TextMesh Pro resources

The installed `com.unity.template.urp-blank` 17.0.14 archive identifies:

```text
com.unity.template.urp-blank copyright © 2021 Unity Technologies ApS

Licensed under the Unity Companion License for Unity-dependent projects.
```

The repository's `Assets/TutorialInfo/Scripts/Readme.cs`, `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`, and `Assets/TutorialInfo/Icons/URP.png` were verified as byte-identical to corresponding files in that installed archive. These are imported Unity content, not original team code. The template's installed license points to the Unity Companion License; preserve its attribution and applicable terms when redistributing its content.

The repository's `Assets/TextMesh Pro/Shaders/TMP_SDF.shader` was verified as byte-identical to the shader in uGUI 2.0.0's TMP Essential Resources archive. Imported TMP resources are not assigned the team's MIT copyright. The separately licensed fonts and EmojiOne artwork are addressed below.

Unity asset paths in this section are relative to `XRProcedualCopilot/`.

## Fonts

### Atkinson Hyperlegible

Copyright 2020 Braille Institute of America, Inc.

The bundled `AtkinsonHyperlegible-Regular.ttf` and its generated SDF font asset remain under **SIL Open Font License 1.1**, not the project's MIT License. The complete license is retained in [Assets/UI/Fonts/OFL.txt](XRProcedualCopilot/Assets/UI/Fonts/OFL.txt).

Retain the copyright and OFL text with font redistributions, including embedded or converted versions. Do not sell the font by itself or relicense it under MIT; follow all OFL conditions concerning modifications and any reserved names.

### Liberation Sans

```text
Digitized data copyright (c) 2010 Google Corporation
    with Reserved Font Arimo, Tinos and Cousine.
Copyright (c) 2012 Red Hat, Inc.
    with Reserved Font Name Liberation.
```

The bundled Liberation Sans font and generated font resources remain under **SIL Open Font License 1.1**. Preserve the complete [Liberation Sans OFL notice](XRProcedualCopilot/Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt), copyright, and applicable reserved-name conditions. These assets are not relicensed under MIT.

## EmojiOne artwork: exact license unresolved

The tracked `XRProcedualCopilot/Assets/TextMesh Pro/Sprites/EmojiOne.png` has an accompanying [EmojiOne Attribution.txt](XRProcedualCopilot/Assets/TextMesh%20Pro/Sprites/EmojiOne%20Attribution.txt). It credits EmojiOne and directs users to its licensing terms but does not identify a precise artwork release or license.

The [upstream legacy license](https://github.com/joypixels/emojione-legacy/blob/master/LICENSE.md) distinguishes artwork from non-artwork and identifies CC BY-SA 4.0 for its artwork. This audit did not establish that the repository's sprite sheet matches that particular release. Do not assume that license, MIT, or any other license applies without verifying the asset's provenance. Confirm the exact version and corresponding attribution/share-alike requirements, or separately authorize replacement/removal.

## External runtimes and services

### Ollama

Ollama is an external runtime, not bundled source or a binary in this repository. Its upstream [LICENSE](https://github.com/ollama/ollama/blob/main/LICENSE) is MIT, with copyright attributed to Ollama. Preserve the applicable notices if redistributing Ollama itself. This does not license models run through Ollama.

### Gemma 3 4B

The companion uses `gemma3:4b`, downloaded separately. Model weights are not tracked in this repository. The installed model's license layer contains **Gemma Terms of Use**, not MIT. That bundled copy identifies a February 21, 2024 modification date; Google's [published terms](https://ai.google.dev/gemma/terms) should also be reviewed for the model/version and use being distributed.

The installed terms address redistribution, downstream use restrictions, providing the agreement, marking modifications, and a notice file for non-hosted distributions. They define distribution to include hosted access. If redistributing Gemma or model derivatives, follow the applicable full agreement; this project's MIT License does not replace it. The notice specified in the installed terms is:

```text
Gemma is provided under and subject to the Gemma Terms of Use found at ai.google.dev/gemma/terms
```

The installed terms distinguish model output from model derivatives. Generated tutorial text still requires a separate check of underlying transcript/media rights and provenance.

### YouTube and Wit.ai services

The official YouTube player and Wit.ai transcription are external services subject to their own terms. Embedding a video does not give the project an MIT license to the video, captions, logos, or other service content. See [YouTube Terms of Service](https://www.youtube.com/t/terms) and [Wit.ai Terms](https://wit.ai/terms).

## Sample media and tutorial provenance

### Downloaded paper-airplane MP4 removed

The downloaded paper-airplane MP4 previously under `XRProcedualCopilot/Assets/Role4/Videos/`, and its Unity `.meta` file, were removed from the current tree in commit `31083f7`. The source was identified as [YouTube video 1PVA2nPU60U](https://www.youtube.com/watch?v=1PVA2nPU60U), but redistribution permission was not verified.

Removal from the current tree does not remove historical Git copies, existing forks, or previously generated APKs. No permission is asserted for redistributing those copies. Older `VideoReview` and `LocalAITest` scenes retain references to the removed asset; this notices document does not alter those scenes.

### Transcripts and generated tutorial data

The airplane transcript in `role3_ai/inputs/` identifies itself as user-supplied text from a YouTube video. Related drafts and tutorial JSON may incorporate externally sourced wording or instructions. Other samples identify themselves as manually prepared or do not provide detailed provenance.

Confirm ownership, permission, or another applicable basis before redistributing externally sourced tutorial text or representing it as team-owned MIT content. Attribution alone does not establish redistribution rights. This inventory does not automatically extend the team's MIT grant to transcripts, video content, or derivative sample data.

## Outstanding distribution checks

- Confirm the exact EmojiOne artwork release and applicable terms.
- Confirm provenance and redistribution rights for externally sourced transcripts and tutorial data.
- Account for the removed MP4 in historical copies and earlier build artifacts; no Git history rewrite was performed.
- Review binary-level dependencies and required notices for any distributed APK, including native libraries. Package-level classifications are not a complete binary audit.
- Ensure required license and attribution texts are actually delivered with redistributed components. A root Markdown file does not automatically make notices accessible inside an APK.
- Preserve the complete upstream license/notice files accompanying dependencies. Unity's ignored package cache is not itself a committed notices bundle.

No third-party rights are granted by this document beyond the rights provided by the respective owners' applicable licenses and permissions.
