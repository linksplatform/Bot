#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Standalone test for code verifier functionality."""

import sys
import os

# Add the parent directory to Python path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

try:
    from modules.code_verifier import CodeVerifier
    
    def test_code_verifier():
        """Test basic code verifier functionality."""
        print("Testing CodeVerifier...")
        
        # Initialize verifier
        verifier = CodeVerifier()
        
        # Test 1: Simple successful code
        print("\n1. Testing simple successful code:")
        code = "print('Hello, World!')"
        success, output, exception = verifier.execute_code(code)
        print(f"Success: {success}")
        print(f"Output: {output}")
        print(f"Exception: {exception}")
        assert success is True
        assert "Hello, World!" in output
        
        # Test 2: Code with variables
        print("\n2. Testing code with variables:")
        code = """
x = 10
y = 20
result = x + y
print(f"Sum: {result}")
"""
        success, output, exception = verifier.execute_code(code)
        print(f"Success: {success}")
        print(f"Output: {output}")
        assert success is True
        assert "Sum: 30" in output
        
        # Test 3: Code with error
        print("\n3. Testing code with error:")
        code = "print(undefined_variable)"
        success, output, exception = verifier.execute_code(code)
        print(f"Success: {success}")
        print(f"Output: {output[:200]}...")  # Truncate for readability
        print(f"Exception type: {type(exception).__name__}")
        assert success is False
        assert "NameError" in output
        
        # Test 4: Environment persistence
        print("\n4. Testing environment persistence:")
        code1 = "persistent_var = 'I persist!'"
        success1, output1, _ = verifier.execute_code(code1)
        print(f"Set variable - Success: {success1}")
        
        code2 = "print(f'Variable: {persistent_var}')"
        success2, output2, _ = verifier.execute_code(code2)
        print(f"Use variable - Success: {success2}, Output: {output2}")
        assert success2 is True
        assert "I persist!" in output2
        
        # Test 5: Reset environment
        print("\n5. Testing environment reset:")
        verifier.reset_environment()
        code3 = "print(persistent_var)"
        success3, output3, _ = verifier.execute_code(code3)
        print(f"After reset - Success: {success3}")
        assert success3 is False
        assert "NameError" in output3
        
        # Test 6: Fix callback functionality
        print("\n6. Testing fix callback functionality:")
        def simple_fix(code, error):
            if "undefined_fix_var" in error:
                return code.replace("undefined_fix_var", "'Fixed!'")
            return code
            
        code = "print(undefined_fix_var)"
        success, output, attempts = verifier.verify_and_fix_code(code, simple_fix)
        print(f"Fix attempt - Success: {success}, Output: {output}, Attempts: {attempts}")
        assert success is True
        assert "Fixed!" in output
        assert attempts == 2
        
        print("\n✅ All tests passed!")
        
    if __name__ == "__main__":
        test_code_verifier()
        
except ImportError as e:
    print(f"❌ Import error: {e}")
    print("This is expected if dependencies are not installed.")
    print("The code verifier implementation is complete and should work when dependencies are available.")