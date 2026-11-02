# 專案進度與待優化紀錄 (Project Notes & Known Issues)

## 📌 當前功能進度 (Completed Features)
1. **操場與跑道 (Athletics Track)**：
   - 400 公尺標準規格跑道，6 道紅色 PU 顆粒材質（`Track_Polyurethane.png`）。
   - 包含直道、半圓彎道、7 條分道白線、終點線與起跑梯次標線。
   - 8 層水泥看台（紅、藍、黃座椅與鋼構頂棚）、4 座 22 米高桿照明燈塔、司令台旗桿與周邊植栽。

2. **綠草地足球場 (Soccer Pitch)**：
   - 長 100m、寬 64m 標準球場，草皮帶有深淺割草條紋（`Grass_Turf.png`）。
   - 完整白色規線（邊線、底線、中線、中圈、禁區、球門區、點球點、角球弧）。
   - 2 座標準 3D 足球門與網格材質，3 顆物理互動足球（支援盤球、踢球與進球偵測）。

3. **人物模型與探索系統 (Character & Locomotion)**：
   - 匯入 Meshy AI Neutral Studio Stance Biped 模型與 PBR 材質（Albedo, Normal, Metallic + Smoothness）。
   - 綁定 Humanoid Avatar，支援 WASD 移動、Shift 衝刺奔跑（8.5 m/s）、空白鍵跳躍、R 鍵重設位置。
   - 第三人稱 360° 環繞鏡頭（滾輪縮放 2m~14m、障礙物防穿模碰撞偵測）。
   - 即時探索 HUD（速度計、目前區域偵測、操作提示、進球廣播）。

---

## ⚠️ 待修復與優化項目 (Known Issues & Pending Tasks)

### 1. 跳躍動作仍然偏怪 (Jump Action Needs Refinement)
* **現狀問題**：起跳姿態、空中滯空與落地時的動作過渡仍顯生硬，缺乏起跳蓄力、空中伸展與著地緩衝的連貫感。
* **預計優化方向**：
  - 引進專屬的三段式跳躍動畫（Jump_Start 起跳預備、Jump_Loop 空中滯空、Jump_Land 落地吸收緩衝）。
  - 優化垂直速度與重力曲線過渡，提升滯空與下墜的重量感。

### 2. 站立靜止動作仍然不夠自然 (Standing / Idle Action Needs Refinement)
* **目前狀態（2026-11-03）**：姿勢大部分已正常。角色待機時手臂與手掌可正常顯示、雙腳接近併攏並落地，不再出現原本大幅後仰與肢體消失的狀況。
* **剩餘微調**：目前使用角色原生 Walking 動畫的中立幀作為穩定待機姿勢，手腳角度仍可在後續導入專用 Idle 動畫時進一步自然化。
* **預計優化方向**：
  - 導入專屬的高品質人體待機動作（Mixamo 或專業 Humanoid Idle 動畫），包含自然的重心轉移、肩膀手肘微幅放鬆與手指微曲。
