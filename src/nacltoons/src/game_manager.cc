// Copyright (c) 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
#include "game_manager.h"
#include "level_layer.h"
#include "CCLuaEngine.h"

USING_NS_CC;

GameManager* GameManager::sharedManager()
{
  static GameManager* shared_manager = NULL;
  if (!shared_manager)
    shared_manager = new GameManager();
  return shared_manager;
}

void GameManager::CreateLevel()
{
  LevelLayer* level = LevelLayer::create();
  scene_->addChild(level, 1, TAG_LAYER_LEVEL);
  level->LoadLevel(level_number_);
}

void GameManager::Restart()
{
  scene_->removeAllChildren();
  // Recreate the level
  CreateLevel();
}

bool GameManager::LoadGame(const char* folder) {
  CCScriptEngineManager* manager = CCScriptEngineManager::sharedManager();
  CCLuaEngine* engine = (CCLuaEngine*)manager->getScriptEngine();
  assert(engine);
  CCLuaStack* lua_stack = engine->getLuaStack();
  assert(lua_stack);

  CCLog("running LoadGame on stack: %p", lua_stack);
  lua_stack->pushString(folder);

  // Call 'LoadGame' with single argument pushed above.
  // 'LoadGame' is a global symbol defined in loader.lua.
  int rtn = lua_stack->executeFunctionByName("LoadGame", 1);
  assert(rtn != -1);
  if (rtn != 1)
    return false;

  return true;
}

void GameManager::LoadLevel(int level_number)
{
  CCDirector* director = CCDirector::sharedDirector();
  CCTransitionScene* transition;
  level_number_ = level_number;
  scene_ = CCScene::create();

  CreateLevel();

  director->setDepthTest(true);
  transition = CCTransitionPageTurn::create(1.0f, scene_, false);
  director->pushScene(transition);
}

void GameManager::GameOver(bool success) {
  CCSize visible_size = CCDirector::sharedDirector()->getVisibleSize();

  // Create a black overlay layer with success/failure message
  CCLayer* overlay = CCLayerColor::create(ccc4(0x0, 0x0, 0x0, 0x0));
  const char* text = success ? "Success!" : "Failure!";
  CCLabelTTF* label = CCLabelTTF::create(text, "Arial.ttf", 24);
  label->setPosition(ccp(visible_size.width/2, visible_size.height/2));
  overlay->addChild(label);
  scene_->addChild(overlay, 2, TAG_LAYER_OVERLAY);

  // Face the overlay layer into to 50%
  CCActionInterval* fadein = CCFadeTo::create(0.5f, 0x7F);
  overlay->runAction(fadein);
}
