# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - main [ref=e2]:
    - generic [ref=e3]:
      - heading "Winged Bean Docs" [level=1] [ref=e4]
      - paragraph [ref=e5]: Interactive terminal demos are powered by the embedded Asciinema player below.
    - generic [ref=e6]:
      - heading "Sample Terminal Session" [level=2] [ref=e7]
      - generic [ref=e8]:
        - generic [ref=e9]: "Select a demo:"
        - combobox "Select a demo:" [ref=e10]:
          - option "Hello Example" [selected]
          - option "NPM Install"
          - option "Git Commit"
          - option "Docker Build"
      - generic [ref=e13]:
        - generic [ref=e15]:
          - button "Play" [ref=e17] [cursor=pointer]
          - textbox [ref=e19]:
            - generic [ref=e20]: "--:--"
          - button "Toggle fullscreen mode" [ref=e24] [cursor=pointer]:
            - img [ref=e25] [cursor=pointer]
        - generic [ref=e29]: 💥
    - generic [ref=e30]:
      - heading "Live Terminal (PTY via node-pty) ⭐ Recommended" [level=2] [ref=e31]
      - paragraph [ref=e32]: Connect to Terminal.Gui v2 running inside a real PTY spawned by Node.js (node-pty) on port 4041. The PTY service is managed by PM2 and automatically spawns the Terminal.Gui application. This uses binary streaming for full terminal compatibility.
      - generic [ref=e38]:
        - textbox "Terminal input" [ref=e39]
        - generic [ref=e40]: WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW
    - paragraph [ref=e68]:
      - text: Replace
      - code [ref=e69]: public/*.cast
      - text: with your own recordings and reuse the
      - code [ref=e70]: AsciinemaPlayer
      - text: component across pages.
  - generic [ref=e72]: qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq
```
