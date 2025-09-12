#include <gtest/gtest.h>

namespace Platform::Bot::Tests
{
    TEST(BotTests, BasicTest)
    {
        EXPECT_TRUE(true);
    }
}

int main(int argc, char **argv)
{
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}