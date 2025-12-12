using UnityEngine;
using System.Collections.Generic;

public class Orca {

    public void Execute(float timeHorizonObst, float timeHorizon, float timeStep, List<RvoAgent> agents) {
        foreach (RvoAgent agent in agents) {
            agent.Execute(timeHorizonObst, timeHorizon, timeStep, agents);
        }
    }

    private static List<AABBObstacle> obstacles = new List<AABBObstacle>();

    public Orca() {
        obstacles.Clear();
    }

    public void AddObstacle(AABB_Rect aabb) {
        Obstacle o0 = new Obstacle { point = new Vector2(aabb.minX, aabb.minY) };
        Obstacle o1 = new Obstacle { point = new Vector2(aabb.maxX, aabb.minY) };
        Obstacle o2 = new Obstacle { point = new Vector2(aabb.maxX, aabb.maxY) };
        Obstacle o3 = new Obstacle { point = new Vector2(aabb.minX, aabb.maxY) };

        o0.next = o1; o0.previous = o3;
        o1.next = o2; o1.previous = o0;
        o2.next = o3; o2.previous = o1;
        o3.next = o0; o3.previous = o2;

        o0.direction = (o1.point - o0.point).normalized; // → (1, 0)
        o1.direction = (o2.point - o1.point).normalized; // → (0, 1)
        o2.direction = (o3.point - o2.point).normalized; // → (-1, 0)
        o3.direction = (o0.point - o3.point).normalized; // → (0, -1)

        o0.convex = true;
        o1.convex = true;
        o2.convex = true;
        o3.convex = true;

        Obstacle[] result = new Obstacle[4];
        result[0] = o0;
        result[1] = o1;
        result[2] = o2;
        result[3] = o3;

        AABBObstacle aabbObstacle = new AABBObstacle { minX = aabb.minX, minY = aabb.minY, maxX = aabb.maxX, maxY = aabb.maxY, obstacles = result };

        obstacles.Add(aabbObstacle);
    }

    public static List<Obstacle> QueryObstacles(Vector2 position, float radius) {
        List<Obstacle> result = new List<Obstacle>();
        float radiusSq = radius * radius;
        for (int i = 0; i < obstacles.Count; ++i) {
            AABBObstacle aabb = obstacles[i];
            // 计算圆心相对于AABB中心的偏移量
            float closestX = Mathf.Clamp(position.x, aabb.minX, aabb.maxX);
            float closestY = Mathf.Clamp(position.y, aabb.minY, aabb.maxY);

            // 计算最近点到圆心的距离平方
            float distanceX = position.x - closestX;
            float distanceY = position.y - closestY;

            // 判断距离是否小于等于半径的平方
            if (distanceX * distanceX + distanceY * distanceY > radiusSq) continue;

            for (int j = 0; j < aabb.obstacles.Length; ++j) {
                result.Add(aabb.obstacles[j]);
            }
        }
        return result;
    }

}

public class RvoAgent {
    public Vector2 position;
    public Vector2 velocity;
    public float radius;

    public float maxSpeed;
    public Vector2 prefVelocity;

    public Vector2 newVelocity;

    private List<Line> orcaLines = new List<Line>();

    public RvoAgent(Vector2 position, Vector2 velocity, float radius, float maxSpeed) {
        this.position = position;
        this.velocity = velocity;
        this.radius = radius;
        this.maxSpeed = maxSpeed;
        this.prefVelocity = velocity;
        this.newVelocity = velocity;
    }

    public void Execute(float timeHorizonObst, float timeHorizon, float timeStep, List<RvoAgent> agents) {

        orcaLines.Clear();

        float invTimeHorizonObst = 1.0f / timeHorizonObst;

        List<Obstacle> obstacles = Orca.QueryObstacles(position, radius * 1.5f);

        for (int i = 0; i < obstacles.Count; ++i) {

            Obstacle obstacle1 = obstacles[i];
            Obstacle obstacle2 = obstacle1.next;

            Vector2 relativePosition1 = obstacle1.point - position;
            Vector2 relativePosition2 = obstacle2.point - position;
            bool alreadyCovered = false;

            for (int j = 0; j < orcaLines.Count; ++j) {
                if (RvoMath.Det(invTimeHorizonObst * relativePosition1 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -RvoMath.RVO_EPSILON
                        && RvoMath.Det(invTimeHorizonObst * relativePosition2 - orcaLines[j].point, orcaLines[j].direction) - invTimeHorizonObst * radius >= -RvoMath.RVO_EPSILON) {
                    alreadyCovered = true;
                    break;
                }
            }

            if (alreadyCovered) {
                continue;
            }

            float distSq1 = RvoMath.AbsSq(relativePosition1);
            float distSq2 = RvoMath.AbsSq(relativePosition2);

            float radiusSq = RvoMath.Sqr(radius);

            Vector2 obstacleVector = obstacle2.point - obstacle1.point;
            float s = RvoMath.Dot(-relativePosition1, obstacleVector) / RvoMath.AbsSq(obstacleVector);
            float distSqLine = RvoMath.AbsSq(-relativePosition1 - s * obstacleVector);

            Line line = new Line();

            if (s < 0.0f && distSq1 <= radiusSq) {
                if (obstacle1.convex) {
                    //line.point = new Vector2(0.0f, 0.0f);
                    //line.direction = RvoMath.normalize(new Vector2(-relativePosition1.y, relativePosition1.x));

                    Vector2 normal = RvoMath.normalize(relativePosition1);
                    line.point = invTimeHorizonObst * relativePosition1 + invTimeHorizonObst * radius * normal;
                    line.direction = new Vector2(-normal.y, normal.x);

                    orcaLines.Add(line);
                }
                continue;
            } else if (s > 1.0f && distSq2 <= radiusSq) {
                if (obstacle2.convex && RvoMath.Det(relativePosition2, obstacle2.direction) >= 0.0f) {
                    //line.point = new Vector2(0.0f, 0.0f);
                    //line.direction = obstacle1.direction;

                    Vector2 normal = RvoMath.normalize(relativePosition2);
                    line.point = invTimeHorizonObst * relativePosition2 + invTimeHorizonObst * radius * normal;
                    line.direction = new Vector2(-normal.y, normal.x);

                    orcaLines.Add(line);
                }
                continue;
            } else if (s >= 0.0f && s <= 1.0f && distSqLine <= radiusSq) {
                //line.point = new Vector2(0.0f, 0.0f);

                Vector2 closest = obstacle1.point + s * (obstacle2.point - obstacle1.point);
                Vector2 normal = new Vector2(obstacle1.direction.y, -obstacle1.direction.x);
                line.point = (closest - position) *  invTimeHorizonObst + invTimeHorizonObst * radius * normal;

                line.direction = obstacle1.direction;
                orcaLines.Add(line);
                continue;
            }

            Vector2 leftLegDirection, rightLegDirection;

            if (s < 0.0f && distSqLine <= radiusSq) {
                if (!obstacle1.convex) {
                    continue;
                }

                obstacle2 = obstacle1;

                float leg1 = RvoMath.Sqrt(distSq1 - radiusSq);
                leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius, -relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
            } else if (s > 1.0f && distSqLine <= radiusSq) {
                if (!obstacle2.convex) {
                    continue;
                }

                obstacle1 = obstacle2;

                float leg2 = RvoMath.Sqrt(distSq2 - radiusSq);
                leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius, relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
            } else {
                if (obstacle1.convex) {
                    float leg1 = RvoMath.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                } else {
                    leftLegDirection = -obstacle1.direction;
                }

                if (obstacle2.convex) {
                    float leg2 = RvoMath.Sqrt(distSq2 - radiusSq);
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                } else {
                    rightLegDirection = obstacle1.direction;
                }
            }

            Obstacle leftNeighbor = obstacle1.previous;

            bool isLeftLegForeign = false;
            bool isRightLegForeign = false;

            if (obstacle1.convex && RvoMath.Det(leftLegDirection, -leftNeighbor.direction) >= 0.0f) {
                leftLegDirection = -leftNeighbor.direction;
                isLeftLegForeign = true;
            }

            if (obstacle2.convex && RvoMath.Det(rightLegDirection, obstacle2.direction) <= 0.0f) {
                rightLegDirection = obstacle2.direction;
                isRightLegForeign = true;
            }

            Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point - position);
            Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point - position);
            Vector2 cutOffVector = rightCutOff - leftCutOff;

            float t = obstacle1 == obstacle2 ? 0.5f : RvoMath.Dot((velocity - leftCutOff), cutOffVector) / RvoMath.AbsSq(cutOffVector);
            float tLeft = RvoMath.Dot((velocity - leftCutOff), leftLegDirection);
            float tRight = RvoMath.Dot((velocity - rightCutOff), rightLegDirection);

            if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f)) {
                Vector2 unitW = RvoMath.normalize(velocity - leftCutOff);

                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = leftCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);

                continue;
            } else if (t > 1.0f && tRight < 0.0f) {
                Vector2 unitW = RvoMath.normalize(velocity - rightCutOff);

                line.direction = new Vector2(unitW.y, -unitW.x);
                line.point = rightCutOff + radius * invTimeHorizonObst * unitW;
                orcaLines.Add(line);

                continue;
            }

            float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (leftCutOff + t * cutOffVector));
            float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (leftCutOff + tLeft * leftLegDirection));
            float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RvoMath.AbsSq(velocity - (rightCutOff + tRight * rightLegDirection));

            if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight) {
                line.direction = -obstacle1.direction;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                orcaLines.Add(line);

                continue;
            }

            if (distSqLeft <= distSqRight) {
                if (isLeftLegForeign) {
                    continue;
                }

                line.direction = leftLegDirection;
                line.point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                orcaLines.Add(line);

                continue;
            }

            if (isRightLegForeign) {
                continue;
            }

            line.direction = -rightLegDirection;
            line.point = rightCutOff + radius * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
            orcaLines.Add(line);
        }

        int numObstLines = orcaLines.Count;

        //float invTimeHorizon = 1.0f / timeHorizon;
        //float invTimeStep = 1.0f / timeStep;

        //List<RvoAgent> otherAgents = new List<RvoAgent>();
        //for(int i = 0; i < agents.Count; i++) {
        //    Vector2 v = position - agents[i].position;
        //    if (RvoMath.AbsSq(v) > radius * radius * 4) continue;
        //    otherAgents.Add(agents[i]);
        //}
        //for (int i = 0; i < otherAgents.Count; i++) {
        //    RvoAgent otherAgent = otherAgents[i];
        //    if (otherAgent == this) continue;
        //    Vector2 relativePosition = otherAgent.position - position;
        //    Vector2 relativeVelocity = velocity - otherAgent.velocity;
        //    float distSq = relativePosition.x * relativePosition.x + relativePosition.y * relativePosition.y;
        //    float combinedRadius = radius + otherAgent.radius;
        //    float combinedRadiusSq = combinedRadius * combinedRadius;

        //    Line line = new Line();
        //    Vector2 u = new Vector2(0, 0);

        //    if (distSq > combinedRadiusSq) {
        //        Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;
        //        float wLengthSq = RvoMath.Dot(w, w);
        //        float dotProduct1 = RvoMath.Dot(w, relativePosition);
        //        if (dotProduct1 < 0.0f && RvoMath.Sqr(dotProduct1) > combinedRadiusSq * wLengthSq) {
        //            float wLength = RvoMath.Sqrt(wLengthSq);
        //            Vector2 unitW = w / wLength;
        //            line.direction = new Vector2(unitW.y, -unitW.x);
        //            u = (combinedRadius * invTimeHorizon - wLength) * unitW;
        //        } else {
        //            float leg = RvoMath.Sqrt(distSq - combinedRadiusSq);
        //            if (RvoMath.Det(relativePosition, w) > 0.0f) {
        //                line.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius,
        //                    relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
        //            } else {
        //                line.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius,
        //                    relativePosition.y * leg - relativePosition.x * combinedRadius) / distSq;
        //            }
        //            float dotProduct2 = RvoMath.Dot(relativeVelocity, line.direction);
        //            u = dotProduct2 * line.direction - relativeVelocity;
        //        }
        //    } else {
        //        Vector2 w = relativeVelocity - invTimeStep * relativePosition;
        //        float wLength = RvoMath.Sqrt(w.x * w.x + w.y * w.y);
        //        if (wLength > 0.0f) {
        //            Vector2 unitW = w / wLength;
        //            line.direction = new Vector2(unitW.y, -unitW.x);
        //            u = (combinedRadius * invTimeStep - wLength) * unitW;
        //        } else {
        //            line.direction = new Vector2(1, 0);
        //            u = new Vector2(combinedRadius * invTimeStep, 0);
        //        }
        //    }

        //    line.point = velocity + 0.5f * u;
        //    orcaLines.Add(line);
        //}
        int lineFail = linearProgram2(orcaLines, maxSpeed, prefVelocity, true, ref newVelocity);

        if (lineFail < orcaLines.Count) {
            linearProgram3(orcaLines, numObstLines, lineFail, maxSpeed, ref newVelocity);
        }
        velocity = newVelocity;
    }

    private int linearProgram2(List<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
        if (directionOpt) {
            result = optVelocity * radius;
        } else if (RvoMath.AbsSq(optVelocity) > RvoMath.Sqr(radius)) {
            result = RvoMath.normalize(optVelocity) * radius;
        } else {
            result = optVelocity;
        }

        for (int i = 0; i < lines.Count; ++i) {
            if (RvoMath.Det(lines[i].direction, lines[i].point - result) > 0.0f) {
                Vector2 tempResult = result;
                if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result)) {
                    result = tempResult;
                    return i;
                }
            }
        }
        return lines.Count;
    }

    private bool linearProgram1(List<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
        float dotProduct = RvoMath.Dot(lines[lineNo].point, lines[lineNo].direction);
        float discriminant = RvoMath.Sqr(dotProduct) + RvoMath.Sqr(radius) - RvoMath.AbsSq(lines[lineNo].point);

        if (discriminant < 0.0f) {
            return false;
        }

        float sqrtDiscriminant = RvoMath.Sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i) {
            float denominator = RvoMath.Det(lines[lineNo].direction, lines[i].direction);
            float numerator = RvoMath.Det(lines[i].direction, lines[lineNo].point - lines[i].point);

            if (RvoMath.fabs(denominator) <= RvoMath.RVO_EPSILON) {
                if (numerator < 0.0f) {
                    return false;
                }
                continue;
            }

            float t = numerator / denominator;

            if (denominator >= 0.0f) {
                tRight = Mathf.Min(tRight, t);
            } else {
                tLeft = Mathf.Max(tLeft, t);
            }

            if (tLeft > tRight) {
                return false;
            }
        }

        if (directionOpt) {
            if (RvoMath.Dot(optVelocity, lines[lineNo].direction) > 0.0f) {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            } else {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            }
        } else {
            float t = RvoMath.Dot(lines[lineNo].direction, (optVelocity - lines[lineNo].point));
            if (t < tLeft) {
                result = lines[lineNo].point + tLeft * lines[lineNo].direction;
            } else if (t > tRight) {
                result = lines[lineNo].point + tRight * lines[lineNo].direction;
            } else {
                result = lines[lineNo].point + t * lines[lineNo].direction;
            }
        }

        return true;
    }

    private void linearProgram3(List<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result) {
        float distance = 0.0f;

        List<Line> projLines = new List<Line>();
        for (int i = beginLine; i < lines.Count; ++i) {
            if (RvoMath.Det(lines[i].direction, lines[i].point - result) > distance) {
                projLines.Clear();
                for (int ii = 0; ii < numObstLines; ++ii) {
                    projLines.Add(lines[ii]);
                }

                for (int j = numObstLines; j < i; ++j) {
                    Line line = new Line();
                    float determinant = RvoMath.Det(lines[i].direction, lines[j].direction);
                    if (RvoMath.fabs(determinant) <= RvoMath.RVO_EPSILON) {
                        if (RvoMath.Dot(lines[i].direction, lines[j].direction) > 0.0f) {
                            continue;
                        } else {
                            line.point = 0.5f * (lines[i].point + lines[j].point);
                        }
                    } else {
                        line.point = lines[i].point + (RvoMath.Det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                    }

                    line.direction = RvoMath.normalize(lines[j].direction - lines[i].direction);
                    projLines.Add(line);
                }

                Vector2 tempResult = result;
                if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count) {
                    result = tempResult;
                }

                distance = RvoMath.Det(lines[i].direction, lines[i].point - result);
            }
        }
    }
}

public class Line {
    public Vector2 point;
    public Vector2 direction;
}

public class Obstacle {
    public Vector2 point;
    public Vector2 direction;
    public bool convex;
    public Obstacle previous;
    public Obstacle next;
}

public class AABBObstacle {
    public float minX, minY, maxX, maxY;
    public Obstacle[] obstacles;
}

public class RvoMath {
    public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
    public static float Det(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    public static float Sqr(float x) => x * x;
    public static float Sqrt(float x) => Mathf.Sqrt(x);

    public static float AbsSq(Vector2 v) => v.x * v.x + v.y * v.y;
    public static Vector2 normalize(Vector2 v) {
        float mag = RvoMath.Sqrt(RvoMath.AbsSq(v));
        return mag > 0.0001f ? v / mag : Vector2.right;
    }

    public const float RVO_EPSILON = 1e-5f;
    public static float fabs(float x) => x < 0 ? -x : x;
}