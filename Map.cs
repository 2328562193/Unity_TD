using System.Collections.Generic;
using UnityEngine;

public class Tile : Recycleable {

    private PoolManager poolManager { get => GameManage.instance.poolManager;}

    private SpriteRenderer spriteRenderer;

    private GameObject go;

    public override void AfterBuild() {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void BuildInit(TileModle model) {
        if(((model.x + model.y) & 1) == 0) {
            this.spriteRenderer.color = new Color(40.0f/255, 40.0f / 255, 40.0f / 255, 1f);
        } else {
            this.spriteRenderer.color = new Color(50.0f / 255, 50.0f / 255, 50.0f / 255, 1f);
        }
        int tileId = model.type & ((1 << 21) - 1);
        if (GridManager.TILES.ContainsKey(tileId)) {
            go = poolManager.GetGameObject(GridManager.TILES[tileId], this.transform.position + new Vector3(0.5f, 0.5f), this.transform.rotation);
            go.transform.SetParent(this.transform);
        }
    }
}


[System.Serializable]
public class TileObj{

    public TileModle model;

    private int width;
    private int height;

    public long towerId { get; private set; }
    public TowerType towerType;

    private int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1, 0 };
    private int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1, 0 };

    public TileObj(TileModle model, int width, int height) {
        this.model = model;
        this.width = width;
        this.height = height;
        this.towerId = -1;
    }

    public int GetIndex() => this.model.x + this.model.y * this.width;
    public bool CanPass() => this.model.CanPass;

    public bool CanTower() {
        if(!this.model.CanTower) return false;
        return this.towerId < 0;
    }

    public void SetTower(TowerController tower){
        if(tower == null){
            this.towerId = -1;
            this.towerType = TowerType.None;
        }else{
            this.towerId = tower.Id;
            this.towerType = tower.model.towerType;
        }
    }

    public bool TryAdjoinIndexByDirection(int direction, out int index){
        index = -1;
        if(direction < 0 || direction > 8) return false;
        
        int nx = model.x + dx[direction];
        int ny = model.y + dy[direction];

        if(nx < 0 || nx >= width || ny < 0 || ny >= height) return false;

        index = nx + ny * width;
        return true;
    }
}

[System.Serializable]
public struct TileModle{
    
    public int x;
    public int y;
    public int type;

    public bool CanTower { get => (this.type & (1 << 30)) != 0; }
    public bool CanPass { get => (this.type & (1 << 29)) != 0; }

    public TileModle(int x, int y, int type) {
        this.x = x;
        this.y = y;
        this.type = type;
    }
}

[System.Serializable]
public struct MapFlowPath{

    public int[] direction;
    public int[] path;

    public MapFlowPath(int width, int height, List<TileObj> tiles, TileObj tile) {
        this.path = new int[tiles.Count];
        this.direction = new int[tiles.Count];
        Queue<TileObj> queue = new Queue<TileObj>();
        queue.Enqueue(tile);
        path[tile.GetIndex()] = 1;
        while(queue.Count > 0){
            TileObj curTile = queue.Dequeue();
            for(int i = 0; i < 8; i+=2){
                if (!curTile.TryAdjoinIndexByDirection(i, out int nextIndex))continue;
                TileObj nextTile = tiles[nextIndex];
                if(!nextTile.CanPass()) continue;
                if (path[nextIndex] > 0) continue;
                path[nextIndex] = path[curTile.GetIndex()] + 1;
                queue.Enqueue(nextTile);
            }
        }
        int count = path.Length;
        for (int i = 0; i < count; i++) {
            if (path[i] == 0) {
                path[i] = int.MaxValue;
                continue; 
            }

            TileObj curTile = tiles[i];
            int bestDirection = 8;
            int minPath = path[curTile.GetIndex()];

            for (int dir = 0; dir < 8; dir++) {
                if (!curTile.TryAdjoinIndexByDirection(dir, out int neighborIndex)) continue;
                if (path[neighborIndex] == 0) continue; 
                if (path[neighborIndex] < minPath) {
                    minPath = path[neighborIndex];
                    bestDirection = dir;
                }
            }
            this.direction[i] = bestDirection;
        }
    }

    public int GetPath(int index){
        if(index < 0 || index >= this.path.Length) return int.MaxValue;
        return this.path[index];
    }

    public int GetDirection(int index){
        if(index < 0 || index >= this.direction.Length) return -1;
        return this.direction[index];
    }
}